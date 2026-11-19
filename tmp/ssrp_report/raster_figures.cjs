const fs = require('node:fs');
const path = require('node:path');
const sharp = require('C:/Users/gandh/.cache/codex-runtimes/codex-primary-runtime/dependencies/node/node_modules/sharp');
(async()=>{for(const f of fs.readdirSync(path.join(__dirname,'figures')).filter(f=>f.endsWith('.svg'))){
  const p=path.join(__dirname,'figures',f);
  await sharp(p,{density:160}).png().toFile(p.replace(/\.svg$/,'.png'));
  console.log(f);
}})().catch(e=>{console.error(e);process.exit(1)});
