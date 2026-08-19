#!/usr/bin/env node
// The one entry point for the catalog test harness.
//
// It sets ANYMCP_SERVERS in the environment of the child process, so that no caller
// needs shell specific syntax. `DEBUG=x cmd` works in bash and fails in PowerShell,
// and the main developer uses PowerShell.
//
//   node scripts/run-tests.js                          all layers, whole catalog
//   node scripts/run-tests.js --layer lint             one layer
//   node scripts/run-tests.js --server xquik           one server, all layers
//   node scripts/run-tests.js --server a --server b    more than one server
//   node scripts/run-tests.js --jobs 4                 more parallel dotnet work
//   node scripts/run-tests.js --list                   print the server ids as JSON
//
// npm passes extra arguments after --, for example:
//   npm run test:protocol -- --server xquik
import { spawnSync } from 'child_process';
import { fileURLToPath } from 'url';
import { loadFullCatalog } from '../test/lib/catalog.js';

// Layers run in this order. Lint is first, because it needs no .NET toolchain and
// fails in about a second.
const LAYERS = ['lint', 'build', 'protocol'];

const repoRoot = fileURLToPath(new URL('../', import.meta.url));
process.chdir(repoRoot);

const options = parseArgs(process.argv.slice(2));

if (options.help) {
  process.stdout.write(usage());
  process.exit(0);
}

if (options.list) {
  // The CI workflow reads this to build its matrix, so a new server needs no
  // workflow edit.
  process.stdout.write(JSON.stringify(loadFullCatalog().map(s => s.id)) + '\n');
  process.exit(0);
}

const layers = options.layer === 'all' ? LAYERS : [options.layer];
const env = { ...process.env };
if (options.servers.length > 0) env.ANYMCP_SERVERS = options.servers.join(',');
if (options.jobs) env.ANYMCP_JOBS = String(options.jobs);

for (const layer of layers) {
  const label = options.servers.length > 0
    ? layer + ' (' + options.servers.join(', ') + ')'
    : layer;
  process.stdout.write('\n=== ' + label + ' ===\n');

  const result = spawnSync(
    process.execPath,
    ['--test', '--test-reporter=spec', 'test/' + layer + '.test.js'],
    { cwd: repoRoot, env, stdio: 'inherit' }
  );

  if (result.error) {
    process.stderr.write('failed to start node: ' + result.error.message + '\n');
    process.exit(1);
  }
  if (result.status !== 0) {
    process.stderr.write('\nthe ' + layer + ' layer failed\n');
    process.exit(result.status === null ? 1 : result.status);
  }
}

function parseArgs(argv) {
  const options = { layer: 'all', servers: [], jobs: null, list: false, help: false };

  for (let i = 0; i < argv.length; i++) {
    const arg = argv[i];
    switch (arg) {
      case '--layer':
      case '-l':
        options.layer = argv[++i];
        if (!LAYERS.includes(options.layer)) {
          fail('unknown layer "' + options.layer + '". Use one of: ' + LAYERS.join(', '));
        }
        break;
      case '--server':
      case '-s':
        options.servers.push(...String(argv[++i] || '').split(',').map(s => s.trim()).filter(Boolean));
        break;
      case '--jobs':
      case '-j':
        options.jobs = Number(argv[++i]);
        if (!Number.isInteger(options.jobs) || options.jobs < 1) fail('--jobs needs a whole number of 1 or more');
        break;
      case '--list':
        options.list = true;
        break;
      case '--help':
      case '-h':
        options.help = true;
        break;
      default:
        fail('unknown argument "' + arg + '"');
    }
  }
  return options;
}

function fail(message) {
  process.stderr.write(message + '\n\n' + usage());
  process.exit(2);
}

function usage() {
  return [
    'Usage: node scripts/run-tests.js [options]',
    '',
    '  --layer, -l <name>   one of: ' + LAYERS.join(', ') + ' (default: all, in that order)',
    '  --server, -s <id>    test only this server. Repeat the flag, or give a comma list.',
    '  --jobs, -j <n>       how many servers to build or probe at the same time (default 2)',
    '  --list               print every server id as a JSON array, and stop',
    '  --help, -h           show this text',
    ''
  ].join('\n');
}
