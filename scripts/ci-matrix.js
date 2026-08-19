#!/usr/bin/env node
// Chooses which servers the CI matrix runs, and prints them as a JSON array.
//
// The logic lives here, and not in the workflow file, so that it can be tested on a
// developer machine. It also needs no jq.
//
//   node scripts/ci-matrix.js --all
//   node scripts/ci-matrix.js --only "xquik,image-utility"
//   git diff --name-only base...HEAD | node scripts/ci-matrix.js --changed
//
// Every result is filtered against the catalog that exists now. A pull request that
// deletes a server therefore gives an empty array, and not a leg that cannot run.
import { readFileSync } from 'fs';
import { fileURLToPath } from 'url';
import { loadFullCatalog } from '../test/lib/catalog.js';

// A change to one of these can affect every server, so the whole catalog runs.
const SHARED_PATHS = [
  /^test\//,
  /^scripts\//,
  /^src\/_data\//,
  /^package(-lock)?\.json$/,
  /^\.github\/workflows\/mcp-tests\.yml$/
];

process.chdir(fileURLToPath(new URL('../', import.meta.url)));

const mode = process.argv[2];
const known = loadFullCatalog().map(s => s.id);

let wanted;
switch (mode) {
  case '--all':
    wanted = known;
    break;

  case '--only':
    wanted = splitList(process.argv[3] || '');
    break;

  case '--changed': {
    const changed = splitList(readFileSync(0, 'utf8'), '\n');
    if (changed.some(file => SHARED_PATHS.some(pattern => pattern.test(file)))) {
      process.stderr.write('a shared file changed, so every server runs\n');
      wanted = known;
    } else {
      wanted = changed
        .filter(file => /^mcp\/.+\.cs$/.test(file))
        .map(file => file.slice('mcp/'.length, -'.cs'.length));
    }
    break;
  }

  default:
    process.stderr.write('usage: ci-matrix.js --all | --only <csv> | --changed (paths on stdin)\n');
    process.exit(2);
}

const catalog = new Set(known);
const chosen = [...new Set(wanted)].filter(id => catalog.has(id)).sort();

const dropped = [...new Set(wanted)].filter(id => !catalog.has(id));
if (dropped.length > 0) {
  // A deleted server, or a typo in the manual input.
  process.stderr.write('not in the catalog, so skipped: ' + dropped.join(', ') + '\n');
}

process.stdout.write(JSON.stringify(chosen));

function splitList(text, separator = ',') {
  return text
    .split(separator === '\n' ? /\r?\n/ : separator)
    .map(item => item.trim())
    .filter(Boolean);
}
