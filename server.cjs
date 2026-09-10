const http = require('node:http');
const fs = require('node:fs');
const path = require('node:path');
const {randomUUID} = require('node:crypto');
const root = __dirname;
const origin = 'http://127.0.0.1:4173';
http.createServer((req, res) => {
  const reply = (code, message) => { res.writeHead(code); res.end(message); };
  if (req.method === 'POST' && req.url === '/api/captures') {
    if (req.headers.origin !== origin || req.headers['content-type'] !== 'video/webm') return reply(403, 'Local gameplay recordings only');
    let bytes = 0, rejected = false;
    const chunks = [];
    req.on('data', chunk => {
      if (rejected) return;
      bytes += chunk.length;
      if (bytes > 100 * 1024 * 1024) { rejected = true; chunks.length = 0; reply(413, 'Clip exceeds 100 MB'); return; }
      chunks.push(chunk);
    });
    req.on('end', () => {
      if (rejected) return;
      const data = Buffer.concat(chunks);
      if (data.length < 4 || data.readUInt32BE(0) !== 0x1a45dfa3) return reply(400, 'Expected WebM recording');
      const folder = 'content/raw/' + new Date().toISOString().slice(0, 10) + '_gameplay';
      const relative = folder + '/catmurai-' + randomUUID() + '.webm';
      fs.mkdir(path.join(root, folder), {recursive: true}, error => {
        if (error) return reply(500, 'Cannot create capture folder');
        fs.writeFile(path.join(root, relative), data, {flag: 'wx'}, error => {
          if (error) return reply(500, 'Cannot save capture');
          res.writeHead(201, {'Content-Type': 'application/json'});
          res.end(JSON.stringify({path: '/' + relative}));
        });
      });
    });
    return;
  }
  if (req.method !== 'GET' && req.method !== 'HEAD') return reply(405, 'Method not allowed');
  let url;
  try { url = decodeURIComponent(req.url.split('?')[0]); } catch { return reply(400, 'Invalid path'); }
  const file = path.resolve(root, '.' + (url === '/' ? '/index.html' : url));
  if (!file.startsWith(root + path.sep) || path.relative(root, file).split(path.sep).some(part => part.startsWith('.'))) return reply(403, 'Forbidden');
  fs.readFile(file, (error, data) => {
    if (error) return reply(404, 'Not found');
    res.setHeader('Content-Type', ({'.html':'text/html','.css':'text/css','.js':'text/javascript','.png':'image/png','.webm':'video/webm','.mp4':'video/mp4'})[path.extname(file)] || 'application/octet-stream');
    res.setHeader('Content-Length', data.length);
    res.end(req.method === 'HEAD' ? undefined : data);
  });
}).listen(4173, '127.0.0.1', () => console.log('Catmurai: ' + origin));
