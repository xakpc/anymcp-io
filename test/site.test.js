// The site layer. It builds the site one time, then checks the files that a coding agent
// reads. It needs no .NET, so it runs in about a second, before the build layer.
//
// What each group of assertions protects:
//   - the raw .cs endpoint is byte-identical to the catalog file. This one check catches a
//     missing "| safe", a whitespace-control mistake, and any future switch to displayCode.
//   - no HTML entity reaches a text output. Nunjucks escapes by default, and an escaped
//     apostrophe or angle bracket makes a .cs file that does not compile and JSON that does
//     not parse. Today no description holds a quote, so this failure is latent until it is not.
//   - every install snippet holds "-v" and "q". Without them dotnet run can write build
//     output to stdout, which carries the JSON-RPC stream.
import { test, before, describe } from 'node:test';
import assert from 'node:assert/strict';
import { createHash } from 'crypto';
import { execFileSync } from 'child_process';
import { existsSync, readFileSync } from 'fs';
import { join } from 'path';
import { fileURLToPath } from 'url';
import { loadFullCatalog } from './lib/catalog.js';

const repoRoot = fileURLToPath(new URL('../', import.meta.url));
const site = (...parts) => join(repoRoot, '_site', ...parts);
const read = (...parts) => readFileSync(site(...parts), 'utf8');
const sha = (buf) => createHash('sha256').update(buf).digest('hex');

const ENTITY = /&(lt|gt|amp|quot|#39|#34);/;
const SITE_URL = JSON.parse(
  readFileSync(join(repoRoot, 'src', '_data', 'site.json'), 'utf8')
).url;

const servers = loadFullCatalog();

before(() => {
  // One build for the whole layer. --quiet keeps the parser's progress lines out of the
  // test output; a build failure still throws, because execFileSync checks the exit code.
  execFileSync(process.execPath, ['node_modules/@11ty/eleventy/cmd.cjs', '--quiet'], {
    cwd: repoRoot,
    stdio: 'pipe'
  });
});

describe('the machine-readable endpoints exist', () => {
  for (const name of [
    'llms.txt', 'install.md', 'install/claude-code.md', 'install/codex.md',
    'servers.json', 'robots.txt', 'sitemap.xml', '_headers'
  ]) {
    test(name, () => {
      assert.ok(existsSync(site(name)), `_site/${name} was not written`);
    });
  }
});

describe('each server has a page, a markdown page, and a source file', () => {
  for (const server of servers) {
    test(server.id, () => {
      assert.ok(existsSync(site('servers', server.id, 'index.html')), 'the HTML page is missing');
      assert.ok(existsSync(site('servers', server.id, 'index.md')), 'the markdown page is missing');
      assert.ok(existsSync(site('servers', server.id, server.name)), 'the source file is missing');
    });
  }
});

describe('the raw source endpoint is byte-identical to the catalog file', () => {
  for (const server of servers) {
    test(server.id, () => {
      const source = readFileSync(join(repoRoot, server.path));
      const published = readFileSync(site('servers', server.id, server.name));
      assert.equal(
        sha(published),
        sha(source),
        `${server.path} and /servers/${server.id}/${server.name} differ. ` +
        'A missing "| safe" or a whitespace-control change breaks this.'
      );
    });
  }
});

describe('no HTML entity reaches a text output', () => {
  const files = ['llms.txt', 'install.md', 'install/claude-code.md', 'install/codex.md', 'servers.json']
    .concat(servers.map(s => `servers/${s.id}/index.md`));
  for (const file of files) {
    test(file, () => {
      const match = ENTITY.exec(read(...file.split('/')));
      assert.equal(match, null, match && `found "${match[0]}"; a "| safe" is missing`);
    });
  }
});

describe('the manifest', () => {
  test('parses, and covers the whole catalog', () => {
    const manifest = JSON.parse(read('servers.json'));
    assert.equal(manifest.version, 1);
    assert.equal(manifest.servers.length, servers.length);
    assert.deepEqual(
      manifest.servers.map(s => s.id).sort(),
      servers.map(s => s.id).sort()
    );
  });

  test('holds absolute URLs only, because an agent reads it with no base URL', () => {
    const manifest = JSON.parse(read('servers.json'));
    for (const server of manifest.servers) {
      for (const [key, url] of Object.entries(server.urls)) {
        assert.ok(url.startsWith(SITE_URL), `${server.id}.urls.${key} is not absolute: ${url}`);
      }
    }
  });

  test('gives every server a runnable argv that keeps -v q', () => {
    const manifest = JSON.parse(read('servers.json'));
    for (const server of manifest.servers) {
      assert.equal(server.run.command, 'dotnet');
      assert.deepEqual(server.run.args.slice(-2), ['-v', 'q'], `${server.id} lost -v q`);
      assert.equal(server.run.args[0], 'run');
    }
  });

  test('defaults to a project install, with a path relative to the project', () => {
    const manifest = JSON.parse(read('servers.json'));
    assert.equal(manifest.runtime.scope, 'project');
    for (const server of manifest.servers) {
      assert.equal(server.run.args[1], `./.mcp-servers/${server.file}`,
        `${server.id} does not use the project path`);
    }
  });

  test('names every tool in the snake case that the wire uses', () => {
    const manifest = JSON.parse(read('servers.json'));
    for (const server of manifest.servers) {
      for (const tool of server.tools) {
        assert.match(tool.name, /^[a-z0-9]+(_[a-z0-9]+)*$/, `${tool.methodName} became ${tool.name}`);
      }
    }
  });
});

describe('every install instruction keeps -v q', () => {
  // A server page carries the values, and the client pages carry the file shapes. So the
  // per-server assertions are about the argv and the two links; the JSON body and the
  // --scope ordering moved to the Claude Code page, which is now the only place they exist.
  for (const server of servers) {
    test(`servers/${server.id}/index.md`, () => {
      const page = read('servers', server.id, 'index.md');
      assert.ok(page.includes(`run ./.mcp-servers/${server.name} -v q`),
        'the page does not give the runnable argv');
      assert.ok(page.includes(`${SITE_URL}/servers/${server.id}/${server.name}`),
        'the page does not give the absolute URL of the source file');
      assert.ok(page.includes(`./.mcp-servers/${server.name}`),
        'the page does not use a path relative to the project');
      // An agent that reads only this page must still reach a client page. Without both
      // links it defaults to whichever client it knows, and writes the wrong file.
      for (const client of ['claude-code', 'codex']) {
        assert.ok(page.includes(`${SITE_URL}/install/${client}.md`),
          `the page does not link to the ${client} setup page`);
      }
    });
  }

  test('install.md sends the reader to both client pages', () => {
    const page = read('install.md');
    // install.md is client-neutral now, so it holds the build command and the rule, and no
    // JSON body. The two array elements are asserted on the client pages instead.
    assert.ok(page.includes('-v q'), 'install.md lost -v q');
    assert.ok(page.includes('"-v"') && page.includes('"q"'),
      'install.md does not say that -v and q are two elements');
    for (const client of ['claude-code', 'codex']) {
      assert.ok(page.includes(`${SITE_URL}/install/${client}.md`),
        `install.md does not link to the ${client} setup page`);
    }
  });

  test('install/claude-code.md', () => {
    const page = read('install', 'claude-code.md');
    assert.ok(page.includes('"mcpServers"'), 'the page lost the mcpServers key');
    assert.ok(page.includes('"-v", "q"'), 'the JSON snippet lost -v q');
    assert.ok(page.includes('-v q'), 'a command line lost -v q');
    // A project install is the default. --scope user is the alternative, and it must come
    // after, so an agent that follows the page in order does not change the machine.
    assert.ok(page.includes('--scope project'), 'the page does not install into the project');
    assert.ok(page.indexOf('--scope project') < page.indexOf('--scope user'),
      'the page offers the machine-wide install before the project install');
    assert.ok(page.includes('./.mcp-servers/'), 'the page does not use the project path');
  });

  test('install/codex.md', () => {
    const page = read('install', 'codex.md');
    assert.ok(page.includes('[mcp_servers.'), 'the page lost the mcp_servers table');
    // Codex takes the argv as separate TOML array elements, exactly like the JSON form.
    assert.ok(page.includes('"-v",'), 'the TOML snippet lost -v as its own element');
    assert.ok(page.includes('"q",'), 'the TOML snippet lost q as its own element');
    assert.ok(page.includes('startup_timeout_sec'), 'the page does not raise the start limit');
    assert.ok(page.includes('./.mcp-servers/'), 'the page does not use the project path');
    // The two traps that make Codex ignore a correct .codex/config.toml, with no error.
    assert.ok(page.includes('trust_level'), 'the page does not name the project trust entry');
    assert.ok(page.includes('\\\\?\\'), 'the page does not warn about the extended Windows path');
    // env holds literal text, so env_vars is the only way a committed file names a secret.
    assert.ok(page.includes('env_vars'), 'the page does not give the secret mechanism');
  });
});

describe('the HTML pages agree with the markdown pages', () => {
  test('the setup guide installs into a project, for both clients', () => {
    const guide = read('setup', 'index.html');
    assert.ok(guide.includes('./.mcp-servers/'), 'the setup guide does not use the project path');
    assert.ok(guide.includes('"-v", "q"'), 'the setup guide lost -v q');
    assert.ok(guide.includes('mcpServers'), 'the setup guide does not show the Claude Code key');
    assert.ok(guide.includes('mcp_servers'), 'the setup guide does not show the Codex key');
    assert.ok(guide.includes('trust_level'), 'the setup guide does not name the Codex trust entry');
  });

  for (const server of servers) {
    test(`servers/${server.id}/index.html`, () => {
      const page = read('servers', server.id, 'index.html');
      assert.ok(page.includes(`./.mcp-servers/${server.name}`),
        'the HTML page does not use the project path');
      assert.ok(page.includes('--scope project'), 'the HTML page does not name the project scope');
      // A person on this page picks a client, so both shapes are here rather than linked.
      assert.ok(page.includes(`[mcp_servers.${server.id}]`),
        'the HTML page gives no Codex configuration');
      assert.ok(page.includes('startup_timeout_sec'),
        'the Codex snippet does not raise the start limit');
    });
  }

  test('a committed config never holds a secret value', () => {
    for (const server of servers.filter(s => s.envVars.length)) {
      const page = read('servers', server.id, 'index.html');
      for (const name of server.envVars) {
        // The literal text: "NAME": "${NAME}" -- built by hand so the dollar-brace pair
        // is not read as a template expression.
        const expected = '"' + name + '": "' + '$' + '{' + name + '}"';
        assert.ok(page.includes(expected),
          server.id + ' does not expand ' + name + ' from the environment');
        // Codex expands nothing, so the same guarantee needs env_vars: the name only,
        // and the value comes from the environment of Codex itself.
        assert.ok(page.includes('env_vars = ') && page.includes('"' + name + '"'),
          server.id + ' does not name ' + name + ' through env_vars for Codex');
        assert.equal(page.includes('env = {'), false,
          server.id + ' puts a literal env table in a committed Codex config');
      }
    }
  });
});

describe('the Copy Agent Prompt button', () => {
  // The prompt is the main action of a card and of a detail page, so two templates write
  // it. Both take the text from the agentPrompt filter in .eleventy.js, and this group
  // fails if one of them stops doing that.
  const home = read('index.html');

  for (const server of servers) {
    test(server.id, () => {
      const expected = `Read ${SITE_URL}/servers/${server.id}/index.md and install the ` +
        `${server.id} MCP server into this project.`;

      const detail = read('servers', server.id, 'index.html');
      assert.ok(detail.includes(`data-prompt="${expected}"`),
        'the detail page carries a different prompt');
      assert.ok(home.includes(`data-prompt="${expected}"`),
        'the catalog card carries a different prompt');

      // The prompt is a pointer, and one sentence. Every rule for the agent is on the .md
      // page, so an instruction that appears here belongs there instead. A second sentence
      // needs a period and a space; the periods inside index.md and the host name do not.
      assert.equal(expected.includes('. '), false, 'the prompt holds more than one sentence');
      assert.ok(expected.endsWith('.'), 'the prompt does not end a sentence');
    });
  }

  test('the home page copies no code', () => {
    // A card gives the prompt and a link. The source belongs to the detail page, which
    // shows the block that the copy buttons read.
    assert.equal(home.includes('onclick="copyServerCode'), false,
      'a catalog card still copies the source code');
  });
});

describe('the Cloudflare header rules are published', () => {
  test('_headers sets a text content type for .cs and .md', () => {
    const headers = read('_headers');
    assert.ok(headers.includes('/*.cs'), 'no rule for .cs');
    assert.ok(headers.includes('/*.md'), 'no rule for .md');
    assert.ok(headers.includes('Content-Type: text/plain'), 'no text content type');
  });
});

describe('the code block that the copy buttons read', () => {
  // The buttons on a server page take the code from <code id="server-code">.textContent,
  // instead of a copy of every server embedded in every page. That only holds if the block
  // is escaped: a raw "<" from a generic or a /// <summary> comment becomes a tag, and the
  // browser drops it from textContent. So the block must decode back to displayCode, byte
  // for byte.
  // Nunjucks escapes a backslash to &#92; as well as the five usual characters. A browser
  // decodes all six, so textContent gives the source back; this decoder must match.
  // &amp; goes last, or it would decode an entity that the source itself holds.
  const decode = (html) => html
    .replace(/&lt;/g, '<')
    .replace(/&gt;/g, '>')
    .replace(/&quot;/g, '"')
    .replace(/&#39;/g, "'")
    .replace(/&#92;/g, '\\')
    .replace(/&amp;/g, '&');

  for (const server of servers) {
    test(server.id, () => {
      const page = read('servers', server.id, 'index.html');
      const marker = page.indexOf('id="server-code"');
      assert.notEqual(marker, -1, 'the page has no block with id="server-code"');

      const start = page.indexOf('>', marker) + 1;
      const end = page.indexOf('</code>', start);
      assert.ok(end > start, 'the block does not close');

      assert.equal(decode(page.slice(start, end)), server.displayCode,
        'the block and displayCode differ. A "| safe" on the code block does this.');

      // The block is clipped to 20 lines, so the page gives a button for the rest. The
      // label holds the total, and a lineCount filter that counts something else shows
      // here and nowhere else.
      const lines = server.displayCode.split('\n').length;
      if (lines > 28) {
        assert.ok(page.includes('id="server-code-toggle"'),
          'the long code block has no button to open it');
        assert.ok(page.includes(`Show all ${lines} lines`),
          `the button does not say "Show all ${lines} lines"`);
        assert.ok(page.includes('id="server-code-fade"'),
          'the clipped block has no fade at its foot');
      }
    });
  }
});

describe('the sitemap', () => {
  test('gives no empty lastmod, because nothing invents a date', () => {
    const sitemap = read('sitemap.xml');
    assert.equal(sitemap.includes('<lastmod></lastmod>'), false, 'an empty lastmod is invalid');
    for (const match of sitemap.matchAll(/<lastmod>([^<]*)<\/lastmod>/g)) {
      assert.match(match[1], /^\d{4}-\d{2}-\d{2}$/, `"${match[1]}" is not a date`);
    }
  });
});
