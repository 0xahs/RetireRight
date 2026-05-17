/**
 * Downloads company logos from Logo.dev and saves them to public/logos/.
 * Run once: node scripts/download-logos.mjs
 * Rerun anytime you add new symbols to the watchlist.
 */

import { writeFileSync, mkdirSync } from 'fs';
import { join, dirname } from 'path';
import { fileURLToPath } from 'url';

const __dirname = dirname(fileURLToPath(import.meta.url));
const OUT_DIR = join(__dirname, '../public/logos');

const TOKEN = 'pk_O1su2OoLS4CWwB9SmFlpgg';

const SYMBOLS = {
  MCB:   'mcb.com.pk',
  SYS:   'systemsltd.com',
  HUBC:  'hubpower.com',
  EFERT: 'engrofertilizers.com',
  UBL:   'ubldigital.com',
  MEBL:  'meezanbank.com',
  FFC:   'ffc.com.pk',
  OGDC:  'ogdcl.com',
};

mkdirSync(OUT_DIR, { recursive: true });

for (const [symbol, domain] of Object.entries(SYMBOLS)) {
  const url = `https://img.logo.dev/${domain}?token=${TOKEN}&retina=true`;
  try {
    const res = await fetch(url);
    if (!res.ok) {
      console.warn(`  SKIP ${symbol} (${domain}) — HTTP ${res.status}`);
      continue;
    }
    const contentType = res.headers.get('content-type') ?? '';
    const ext = contentType.includes('png') ? 'png' : contentType.includes('svg') ? 'svg' : 'png';
    const buf = Buffer.from(await res.arrayBuffer());
    const outPath = join(OUT_DIR, `${symbol}.${ext}`);
    writeFileSync(outPath, buf);
    console.log(`  OK  ${symbol} → ${outPath} (${buf.length} bytes)`);
  } catch (err) {
    console.error(`  ERR ${symbol}:`, err.message);
  }
}

console.log('\nDone. Logos saved to public/logos/');
