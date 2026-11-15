// Unity MCP bridge 客户端（TCP 6401, FRAMING=1）
// 用法: node mcp.cjs '<json>'        直接发命令
//       node mcp.cjs @file.json      从文件读命令
//     环境: UNITY_MCP_PORT 覆盖端口
const net = require('net');
const fs = require('fs');

const port = Number(process.env.UNITY_MCP_PORT || 6401);
let arg = process.argv.slice(2).join(' ');
if (!arg) { console.error('need json or @file'); process.exit(2); }
if (arg.startsWith('@')) arg = fs.readFileSync(arg.slice(1).replace(/^@/, ''), 'utf8');
const cmd = JSON.stringify(JSON.parse(arg));

const sock = net.connect(port, '127.0.0.1');
sock.setNoDelay(true);
let buf = Buffer.alloc(0);
let handshaked = false;
let sent = false;
const timer = setTimeout(() => { console.error('TIMEOUT'); sock.destroy(); process.exit(3); }, 300000);

function frame(obj) {
  const payload = Buffer.from(typeof obj === 'string' ? obj : JSON.stringify(obj), 'utf8');
  const head = Buffer.alloc(8);
  head.writeBigUInt64BE(BigInt(payload.length), 0);
  return Buffer.concat([head, payload]);
}

sock.on('connect', () => { });
sock.on('data', (d) => {
  buf = Buffer.concat([buf, d]);
  if (!handshaked) {
    const i = buf.indexOf(0x0a);
    if (i < 0) return;
    const hs = buf.slice(0, i).toString('ascii');
    if (!hs.startsWith('WELCOME')) { console.error('BAD HANDSHAKE: ' + hs); process.exit(4); }
    buf = buf.slice(i + 1);
    handshaked = true;
    if (!sent) { sent = true; sock.write(frame(cmd)); }
  }
  while (buf.length >= 8) {
    const len = Number(buf.readBigUInt64BE(0));
    if (buf.length < 8 + len) break;
    const payload = buf.slice(8, 8 + len).toString('utf8');
    buf = buf.slice(8 + len);
    clearTimeout(timer);
    try { console.log(JSON.stringify(JSON.parse(payload), null, 2)); }
    catch { console.log(payload); }
    sock.end();
  }
});
sock.on('error', (e) => { console.error('ERR ' + e.message); process.exit(5); });
sock.on('close', () => { clearTimeout(timer); });
