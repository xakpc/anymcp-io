// Layer 2: each server compiles, and compiles without a warning.
//
// A warning breaks the copy-and-run promise, because the user sees it on the first run.
// This is why image-utility stays on ImageSharp 3.x. Set ANYMCP_ALLOW_WARNINGS=1 to
// downgrade a warning to a note, for the case where a new SDK adds one that the catalog
// cannot fix at once.
import test from 'node:test';
import assert from 'node:assert/strict';
import { spawn } from 'child_process';
import { loadCatalog, overridesFor } from './lib/catalog.js';

const BUILD_TIMEOUT_MS = Number(process.env.ANYMCP_BUILD_TIMEOUT_MS || 300000);
const JOBS = Math.max(1, Number(process.env.ANYMCP_JOBS || 2));
const ALLOW_WARNINGS = process.env.ANYMCP_ALLOW_WARNINGS === '1';

// "warning CS0168", "warning NU1701", and so on. The bare word "warning" is too common
// in build output to use on its own.
const WARNING_LINE = /\bwarning\s+[A-Z]{2,}\d+/;

const catalog = loadCatalog();

// The builds run first, in a bounded pool, and then one test reports each result.
// Unbounded parallel builds fight over the same package cache, and a strictly serial
// run gets slow once the catalog is large.
const results = await runPool(catalog, JOBS);

test('the catalog holds at least one server', () => {
  assert.ok(catalog.length > 0, 'no .cs file was found in mcp/');
});

for (const server of catalog) {
  const result = results.get(server.id);

  test(server.path, async t => {
    await t.test('dotnet build succeeds', () => {
      assert.ok(!result.timedOut,
        'the build did not finish in ' + BUILD_TIMEOUT_MS + ' ms');
      assert.equal(result.code, 0,
        'dotnet build exited with code ' + result.code + '\n' + indent(result.output));
    });

    await t.test('the build produces no warning', t2 => {
      const warnings = result.output.split('\n').filter(line => WARNING_LINE.test(line));
      if (warnings.length > 0 && ALLOW_WARNINGS) {
        t2.diagnostic('ANYMCP_ALLOW_WARNINGS=1, so these warnings do not fail:\n' +
          indent(warnings.join('\n')));
        return;
      }
      assert.deepEqual(warnings, [],
        'the user sees these warnings on the first run:\n' + indent(warnings.join('\n')));
    });
  });
}

async function runPool(servers, jobs) {
  const queue = [...servers];
  const output = new Map();

  async function worker() {
    for (;;) {
      const server = queue.shift();
      if (!server) return;
      const started = Date.now();
      output.set(server.id, await build(server));
      process.stderr.write(
        '# built ' + server.path + ' in ' + (Date.now() - started) + ' ms\n');
    }
  }

  await Promise.all(Array.from({ length: Math.min(jobs, servers.length) }, worker));
  return output;
}

function build(server) {
  const timeoutMs = overridesFor(server.id).buildTimeoutMs || BUILD_TIMEOUT_MS;

  return new Promise(resolve => {
    // Verbosity "minimal" is the quietest level that still prints warnings.
    const child = spawn('dotnet', ['build', server.path, '-v', 'm', '--nologo'], {
      cwd: process.cwd(),
      env: { ...process.env, DOTNET_NOLOGO: '1', DOTNET_CLI_TELEMETRY_OPTOUT: '1' },
      stdio: ['ignore', 'pipe', 'pipe'],
      windowsHide: true
    });

    let output = '';
    let timedOut = false;
    child.stdout.setEncoding('utf8');
    child.stderr.setEncoding('utf8');
    child.stdout.on('data', chunk => { output += chunk; });
    child.stderr.on('data', chunk => { output += chunk; });

    const timer = setTimeout(() => {
      timedOut = true;
      child.kill('SIGKILL');
    }, timeoutMs);

    child.on('error', err => {
      clearTimeout(timer);
      resolve({ code: -1, output: output + '\nfailed to start dotnet: ' + err.message, timedOut });
    });
    child.on('close', code => {
      clearTimeout(timer);
      resolve({ code, output, timedOut });
    });
  });
}

function indent(text) {
  return text.trim().split('\n').map(line => '    ' + line).join('\n');
}
