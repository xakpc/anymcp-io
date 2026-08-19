// Loads the catalog through the same parser that the site uses.
// Never reimplement parsing here: the tests must see exactly what the pages show.
import { readFileSync, readdirSync } from 'fs';
import { basename, extname, join } from 'path';
import { fileURLToPath } from 'url';
import loadServers from '../../src/_data/servers.js';

const repoRoot = fileURLToPath(new URL('../../', import.meta.url));
const mcpDir = join(repoRoot, 'mcp');

/**
 * Returns every server the site would render, as an array.
 * `servers.js` prints progress with console.log on each call, so it is muted here.
 */
export function loadFullCatalog() {
  const realLog = console.log;
  console.log = () => {};
  let data;
  try {
    data = loadServers();
  } finally {
    console.log = realLog;
  }

  // The parser keys on the front matter id, which is not always the file name.
  // Match each parsed server back to the file that holds it, so that a test can say
  // "the id does not agree with the file name".
  const byContents = new Map();
  for (const file of readdirSync(mcpDir).filter(f => extname(f) === '.cs')) {
    byContents.set(readFileSync(join(mcpDir, file), 'utf8'), file);
  }

  return Object.entries(data).map(([id, server]) => {
    const file = byContents.get(server.code) || `${id}.cs`;
    return { ...server, id, file, stem: basename(file, '.cs'), path: `mcp/${file}` };
  });
}

/**
 * The ids named by ANYMCP_SERVERS, or an empty array when the variable is not set.
 */
export function selectedIds() {
  return (process.env.ANYMCP_SERVERS || '')
    .split(',')
    .map(s => s.trim())
    .filter(Boolean);
}

/**
 * The catalog after the ANYMCP_SERVERS filter.
 * Use this for the per-server tests. Use loadFullCatalog() for the relational tests,
 * because a filtered set cannot show a duplicate id or a version disparity.
 */
export function loadCatalog() {
  const all = loadFullCatalog();
  const only = selectedIds();
  if (only.length === 0) return all;

  const known = new Set(all.map(s => s.id));
  const unknown = only.filter(id => !known.has(id));
  if (unknown.length > 0) {
    throw new Error(
      `ANYMCP_SERVERS names a server that the catalog does not hold: ${unknown.join(', ')}.\n` +
      `Known ids: ${[...known].sort().join(', ')}`
    );
  }
  return all.filter(s => only.includes(s.id));
}

let overridesCache = null;

/**
 * Per-server test settings from the optional test/overrides.json file.
 * A server that the file does not name uses the defaults, so a new server
 * needs no entry.
 */
export function overridesFor(id) {
  if (overridesCache === null) {
    try {
      overridesCache = JSON.parse(readFileSync(new URL('../overrides.json', import.meta.url), 'utf8'));
    } catch {
      overridesCache = {};
    }
  }
  return overridesCache[id] || {};
}
