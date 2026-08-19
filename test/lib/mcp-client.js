// A minimal MCP client that speaks JSON-RPC over stdio to one single-file server.
//
// Two invariants drive the design:
//
//  1. Standard input stays open for the whole session. When stdin reaches end of file the
//     .NET host shuts down at once, and replies that are still in the buffer are lost. A
//     correct server then looks broken. Input closes only in close().
//  2. Standard output carries the protocol and nothing else. Every line that is not a
//     JSON-RPC message goes to strayStdout, and the protocol test fails on it.
import { spawn } from 'child_process';

const DEFAULT_TIMEOUT_MS = 120000;

export class McpStdioClient {
  /**
   * @param {string} filePath path of the .cs server file, relative to cwd
   * @param {{env?: object, scrubEnv?: string[], cwd?: string, timeoutMs?: number}} options
   */
  constructor(filePath, options = {}) {
    this.filePath = filePath;
    this.options = options;
    this.timeoutMs = options.timeoutMs || DEFAULT_TIMEOUT_MS;
    this.strayStdout = [];
    this.messages = [];
    this.stderr = '';
    this.pending = new Map();
    this.nextId = 1;
    this.buffer = '';
    this.exited = false;
    this.exitCode = null;
    this.child = null;
  }

  start() {
    const env = { ...process.env, ...(this.options.env || {}) };
    // Remove every declared secret unless the caller supplies one. A catalog server must
    // start and list its tools with no key present, because that is what a user gets.
    for (const name of this.options.scrubEnv || []) {
      if (!(this.options.env && name in this.options.env)) delete env[name];
    }
    // Keep the .NET tooling quiet so that nothing extra can reach the streams.
    env.DOTNET_NOLOGO = '1';
    env.DOTNET_CLI_TELEMETRY_OPTOUT = '1';

    this.child = spawn('dotnet', ['run', this.filePath, '-v', 'q'], {
      cwd: this.options.cwd || process.cwd(),
      env,
      stdio: ['pipe', 'pipe', 'pipe'],
      windowsHide: true
    });

    this.child.stdout.setEncoding('utf8');
    this.child.stderr.setEncoding('utf8');
    this.child.stdout.on('data', chunk => this._onStdout(chunk));
    this.child.stderr.on('data', chunk => { this.stderr += chunk; });

    this.child.on('error', err => this._failAll(`failed to start dotnet: ${err.message}`));
    this.child.on('exit', code => {
      this.exited = true;
      this.exitCode = code;
      if (this.pending.size > 0) {
        this._failAll(`the server exited with code ${code} before it answered`);
      }
    });
    return this;
  }

  _onStdout(chunk) {
    this.buffer += chunk;
    let index;
    while ((index = this.buffer.indexOf('\n')) >= 0) {
      const line = this.buffer.slice(0, index);
      this.buffer = this.buffer.slice(index + 1);
      this._onLine(line);
    }
  }

  _onLine(rawLine) {
    const line = rawLine.replace(/\r$/, '').trim();
    if (line === '') return;

    let message;
    try {
      message = JSON.parse(line);
    } catch {
      this.strayStdout.push(line);
      return;
    }
    if (typeof message !== 'object' || message === null || message.jsonrpc !== '2.0') {
      this.strayStdout.push(line);
      return;
    }

    this.messages.push(message);
    if (message.id === undefined || message.id === null) return;

    const waiter = this.pending.get(message.id);
    if (waiter) {
      this.pending.delete(message.id);
      clearTimeout(waiter.timer);
      waiter.resolve(message);
    }
  }

  _failAll(reason) {
    for (const [, waiter] of this.pending) {
      clearTimeout(waiter.timer);
      waiter.reject(new Error(this.describe(reason)));
    }
    this.pending.clear();
  }

  /** A failure message that carries the stderr text, where the real cause is. */
  describe(reason) {
    const tail = this.stderr.trim();
    const stray = this.strayStdout.length
      ? `\n  stray stdout (${this.strayStdout.length} line(s)):\n${indent(this.strayStdout.join('\n'))}`
      : '';
    return `${this.filePath}: ${reason}` +
      (tail ? `\n  stderr:\n${indent(tail)}` : '\n  stderr: (empty)') + stray;
  }

  notify(method, params) {
    this._write({ jsonrpc: '2.0', method, ...(params ? { params } : {}) });
  }

  request(method, params, timeoutMs) {
    const id = this.nextId++;
    const limit = timeoutMs || this.timeoutMs;
    return new Promise((resolve, reject) => {
      const timer = setTimeout(() => {
        this.pending.delete(id);
        reject(new Error(this.describe(`no answer to "${method}" in ${limit} ms`)));
      }, limit);
      this.pending.set(id, { resolve, reject, timer });
      this._write({ jsonrpc: '2.0', id, method, params: params || {} });
    });
  }

  _write(message) {
    if (this.exited) throw new Error(this.describe('the server is not running'));
    this.child.stdin.write(JSON.stringify(message) + '\n');
  }

  /** The standard handshake. Returns the initialize result. */
  async initialize(protocolVersion = '2025-06-18') {
    const response = await this.request('initialize', {
      protocolVersion,
      capabilities: {},
      clientInfo: { name: 'anymcp-test-harness', version: '1.0.0' }
    });
    this.notify('notifications/initialized');
    return response;
  }

  /**
   * Ends input, and waits for the process to stop. Returns true when the server exits by
   * itself, and false when the harness had to kill it.
   */
  async close(graceMs = 10000) {
    if (!this.child || this.exited) return true;
    this.child.stdin.end();
    const clean = await new Promise(resolve => {
      const timer = setTimeout(() => resolve(false), graceMs);
      this.child.once('exit', () => { clearTimeout(timer); resolve(true); });
    });
    if (!clean) {
      this.child.kill('SIGKILL');
      await new Promise(resolve => setTimeout(resolve, 500));
    }
    return clean;
  }
}

function indent(text) {
  return text.split('\n').map(l => `    ${l}`).join('\n');
}
