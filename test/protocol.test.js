// Layer 3: each server speaks the protocol.
//
// This layer starts the real server, does the handshake, and asks for the tool list.
// It is the only layer that can prove three things:
//   - stdout carries JSON-RPC and nothing else
//   - the server exposes tools at run time, and not zero (the AOT failure)
//   - the tool list on the catalog page agrees with the tool list on the wire
import test from 'node:test';
import assert from 'node:assert/strict';
import { loadCatalog, loadFullCatalog, overridesFor } from './lib/catalog.js';
import { McpStdioClient } from './lib/mcp-client.js';

const PROTOCOL_VERSION = '2025-06-18';
const STARTUP_TIMEOUT_MS = Number(process.env.ANYMCP_PROTOCOL_TIMEOUT_MS || 300000);
const JOBS = Math.max(1, Number(process.env.ANYMCP_JOBS || 2));

const catalog = loadCatalog();

// Every secret that any server declares is removed from the environment before a server
// starts. A catalog server must list its tools with no key set, because that is what a
// user gets after pasting the file. It also lets a pull request from a fork be gated
// without giving a secret to code that nobody has reviewed yet.
const allDeclaredSecrets = [...new Set(loadFullCatalog().flatMap(s => s.envVars))];

const sessions = await runPool(catalog, JOBS);

test('the catalog holds at least one server', () => {
  assert.ok(catalog.length > 0, 'no .cs file was found in mcp/');
});

for (const server of catalog) {
  const session = sessions.get(server.id);

  test(server.path, async t => {
    await t.test('the server answers initialize with no secret set', () => {
      assert.equal(session.error, null, session.error || '');
      assert.ok(session.init, 'the server sent no initialize result');
      assert.equal(session.init.protocolVersion, PROTOCOL_VERSION,
        'the server answered protocol version "' + session.init.protocolVersion + '"');
      assert.ok(session.init.serverInfo && session.init.serverInfo.name,
        'the initialize result carries no serverInfo.name');
    });

    await t.test('stdout carries only JSON-RPC', () => {
      // One log line, or one line of build output, on stdout breaks the client.
      assert.deepEqual(session.strayStdout, [],
        'these lines reached stdout and are not JSON-RPC messages:\n' +
        indent(session.strayStdout.join('\n')) +
        '\nSend every log line to stderr, and pass "-v q" to dotnet run.');
    });

    await t.test('the server exposes at least one tool', () => {
      // Zero tools with a healthy handshake is the signature of AOT compilation
      // removing the metadata that WithToolsFromAssembly() needs.
      assert.ok(session.wireTools.length > 0,
        'tools/list returned no tool. Check "#:property PublishAot=false".');
    });

    await t.test('every tool on the wire carries a description', () => {
      for (const tool of session.wireTools) {
        assert.ok(tool.description && tool.description.trim().length > 0,
          'tool ' + tool.name + ' has no description. The LLM reads this text.');
      }
    });

    await t.test('the catalog page agrees with the wire', () => {
      // The site reads the tool list from the source text, and the client reads it
      // from the running server. When the two disagree, the page tells a lie.
      // The C# SDK turns GeneratePassword into generate_password, so the comparison
      // ignores case and separators.
      const onPage = server.tools.map(x => x.name);
      const onWire = session.wireTools.map(x => x.name);
      const detail = '\n  page: [' + onPage.join(', ') + ']' +
                     '\n  wire: [' + onWire.join(', ') + ']';

      assert.equal(onWire.length, onPage.length,
        'the page lists ' + onPage.length + ' tool(s) and the server offers ' +
        onWire.length + '.' + detail);
      assert.deepEqual(onWire.map(normalize).sort(), onPage.map(normalize).sort(),
        'the tool names do not match.' + detail);
    });

    await t.test('the server stops when input closes', () => {
      assert.ok(session.cleanExit,
        'the server did not exit after stdin closed, and the harness killed it');
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
      output.set(server.id, await session(server));
      process.stderr.write(
        '# probed ' + server.path + ' in ' + (Date.now() - started) + ' ms\n');
    }
  }

  await Promise.all(Array.from({ length: Math.min(jobs, servers.length) }, worker));
  return output;
}

async function session(server) {
  const override = overridesFor(server.id);
  const client = new McpStdioClient(server.path, {
    timeoutMs: override.startupTimeoutMs || STARTUP_TIMEOUT_MS,
    scrubEnv: allDeclaredSecrets,
    env: override.env || {}
  }).start();

  const result = { init: null, wireTools: [], strayStdout: [], error: null, cleanExit: false };

  try {
    const initResponse = await client.initialize(PROTOCOL_VERSION);
    if (initResponse.error) {
      throw new Error(client.describe('initialize failed: ' + JSON.stringify(initResponse.error)));
    }
    result.init = initResponse.result;

    const listResponse = await client.request('tools/list');
    if (listResponse.error) {
      throw new Error(client.describe('tools/list failed: ' + JSON.stringify(listResponse.error)));
    }
    result.wireTools = listResponse.result.tools || [];
  } catch (error) {
    result.error = error.message;
  }

  result.strayStdout = client.strayStdout;
  result.cleanExit = await client.close();
  return result;
}

function normalize(name) {
  return name.toLowerCase().replace(/[_-]/g, '');
}

function indent(text) {
  return text.split('\n').map(line => '    ' + line).join('\n');
}
