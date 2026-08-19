// Layer 1: metadata and conventions. No .NET toolchain, and no server process.
// This layer stays fast as the catalog grows, so it runs first and gates the rest.
import test from 'node:test';
import assert from 'node:assert/strict';
import { parse as parseYaml } from 'yaml';
import { loadCatalog, loadFullCatalog } from './lib/catalog.js';

// The fields that .lode/catalog/front-matter-schema.md defines. The parser ignores
// anything else, so an unknown key is a silent mistake.
const SCHEMA_FIELDS = new Set([
  'id', 'name', 'description', 'longDescription', 'tags', 'status', 'version',
  'author', 'license', 'downloads', 'lastUpdated', 'createdDate', 'envVars'
]);

const STATUS_VALUES = new Set(['stable', 'beta', 'alpha']);
const EXACT_VERSION = /^\d+\.\d+\.\d+(?:[.-][0-9A-Za-z.-]+)?$/;
const SECRET_LITERAL = /(?:api[_-]?key|apikey|token|secret|password|bearer)\s*=\s*"([^"]{16,})"/i;

const catalog = loadCatalog();

test('the catalog holds at least one server', () => {
  assert.ok(catalog.length > 0, 'no .cs file was found in mcp/');
});

for (const server of catalog) {
  test(server.path, async t => {
    const raw = server.code;
    const frontMatter = readFrontMatter(raw);

    await t.test('the front matter block is present and parses', () => {
      assert.ok(frontMatter.found, 'no "// ---" block at the top of the file');
      assert.ok(frontMatter.data && typeof frontMatter.data === 'object',
        'the front matter is not valid YAML, so every field falls back to a default');
    });

    const meta = frontMatter.data || {};

    await t.test('the id agrees with the file name', () => {
      assert.ok(meta.id, 'the front matter sets no id');
      assert.equal(meta.id, server.stem,
        'id "' + meta.id + '" does not match the file name "' + server.file + '". ' +
        'The id sets the page URL and the data key.');
    });

    await t.test('the name gives a working download', () => {
      // downloadServerFile() finds the server with filename.replace('.cs',''),
      // so any other name breaks the download button.
      assert.equal(server.name, server.id + '.cs',
        'name is "' + server.name + '" but must be "' + server.id + '.cs"');
    });

    await t.test('the card fields are set', () => {
      assert.ok(meta.description, 'the front matter sets no description');
      assert.notEqual(server.description, 'MCP Server',
        'description is the parser default, so the card shows nothing useful');
      assert.ok(Array.isArray(server.tags) && server.tags.length > 0,
        'tags is empty, so search cannot find this server');
      assert.ok(meta.version, 'the front matter sets no version');
    });

    await t.test('the status is a value the badge understands', () => {
      assert.ok(STATUS_VALUES.has(server.status),
        'status "' + server.status + '" is not one of ' + [...STATUS_VALUES].join(', '));
    });

    await t.test('the front matter holds no unknown field', () => {
      const unknown = Object.keys(meta).filter(k => !SCHEMA_FIELDS.has(k));
      assert.deepEqual(unknown, [],
        'the parser ignores these fields: ' + unknown.join(', ') +
        '. See .lode/catalog/front-matter-schema.md.');
    });

    await t.test('AOT compilation is off', () => {
      // WithToolsFromAssembly() finds the tools by reflection. AOT removes the metadata
      // that reflection needs, and the server then starts but shows no tool at all.
      assert.match(raw, /^#:property\s+PublishAot\s*=\s*false\s*$/m,
        'the file has no "#:property PublishAot=false" line');
    });

    await t.test('every package pins an exact version', () => {
      const packages = readPackages(raw);
      assert.ok(packages.length > 0, 'the file declares no #:package directive');
      for (const pkg of packages) {
        assert.ok(pkg.version,
          '#:package ' + pkg.name + ' has no version. ' +
          'A user must get the same build months later.');
        assert.match(pkg.version, EXACT_VERSION,
          '#:package ' + pkg.name + '@' + pkg.version + ' is not an exact version');
      }
    });

    await t.test('all logs go to stderr', () => {
      // Stdio transport uses stdout for protocol messages. One log line on stdout
      // damages the stream.
      assert.match(raw, /LogToStandardErrorThreshold/,
        'the console logger does not redirect to stderr');
    });

    await t.test('the site parser finds every tool', () => {
      const attributes = (raw.match(/\[McpServerTool\b/g) || []).length;
      assert.ok(attributes > 0, 'the file declares no [McpServerTool] method');
      assert.ok(server.tools.length > 0,
        'the parser found no tool, so the detail page shows an empty tool list');
      assert.equal(server.tools.length, attributes,
        'the file declares ' + attributes + ' tool(s) but the parser found ' +
        server.tools.length + ': [' + server.tools.map(x => x.name).join(', ') + ']. ' +
        'Keep [McpServerTool] and Description(...) on one line, and put the ' +
        'public static signature within 10 lines below it.');
    });

    await t.test('every tool carries a description', () => {
      for (const tool of server.tools) {
        assert.ok(tool.description && tool.description.trim().length > 0,
          'tool ' + tool.name + ' has an empty description. The LLM reads this text.');
      }
    });

    await t.test('secrets come from the environment, and envVars lists them', () => {
      const used = new Set(
        [...raw.matchAll(/GetEnvironmentVariable\(\s*"([^"]+)"/g)].map(m => m[1])
      );
      const declared = new Set(server.envVars);

      for (const name of used) {
        assert.ok(declared.has(name),
          'the code reads ' + name + ' but envVars does not list it, so the .mcp.json ' +
          'snippet on the page misses its env block');
      }
      for (const name of declared) {
        assert.ok(used.has(name),
          'envVars lists ' + name + ' but the code never reads it');
      }

      const leak = raw.match(SECRET_LITERAL);
      assert.equal(leak, null,
        leak ? 'a secret looks hard coded: ' + leak[0].slice(0, 60) : '');
    });
  });
}

// Relational tests. These always use the full catalog, because a filtered subset
// cannot show a duplicate id or a version disparity.
const fullCatalog = loadFullCatalog();

test('every id is unique', () => {
  const seen = new Map();
  for (const server of fullCatalog) {
    const previous = seen.get(server.id);
    assert.equal(previous, undefined,
      'id "' + server.id + '" is in both ' + previous + ' and ' + server.path +
      '. The id is the data key, so one entry overwrites the other.');
    seen.set(server.id, server.path);
  }
});

test('one package never has two versions in the catalog', () => {
  const versions = new Map(); // package name -> Map(version -> [paths])
  for (const server of fullCatalog) {
    for (const pkg of readPackages(server.code)) {
      if (!versions.has(pkg.name)) versions.set(pkg.name, new Map());
      const byVersion = versions.get(pkg.name);
      if (!byVersion.has(pkg.version)) byVersion.set(pkg.version, []);
      byVersion.get(pkg.version).push(server.path);
    }
  }

  const conflicts = [];
  for (const [name, byVersion] of versions) {
    if (byVersion.size > 1) {
      const detail = [...byVersion.entries()]
        .map(([version, paths]) => '    ' + version + ' in ' + paths.join(', '))
        .join('\n');
      conflicts.push('  ' + name + ':\n' + detail);
    }
  }
  assert.deepEqual(conflicts, [],
    'these packages have more than one version in the catalog:\n' + conflicts.join('\n'));
});

function readFrontMatter(contents) {
  const lines = contents.split('\n');
  const collected = [];
  let inside = false;
  let closed = false;

  for (const rawLine of lines) {
    const line = rawLine.trim();
    if (line === '// ---') {
      if (!inside) { inside = true; continue; }
      closed = true;
      break;
    }
    if (inside && line.startsWith('//')) collected.push(line.substring(2));
  }

  let data = null;
  if (closed) {
    try { data = parseYaml(collected.join('\n')) || {}; } catch { data = null; }
  }
  return { found: inside && closed, data };
}

function readPackages(contents) {
  return [...contents.matchAll(/^#:package\s+(\S+)\s*$/gm)].map(match => {
    const at = match[1].lastIndexOf('@');
    return at > 0
      ? { name: match[1].slice(0, at), version: match[1].slice(at + 1) }
      : { name: match[1], version: null };
  });
}
