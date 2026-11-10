// 用法: node setlnkaumid.js <lnk路径> <AUMID>
// 纯二进制读写 .lnk 的 PropertyStoreDataBlock，写入/替换 System.AppUserModel.ID (pid=5)
// 不依赖 COM 属性系统（本机 SHGetPropertyStoreFromParsingName 全线 E_NOINTERFACE）。
const fs = require('fs');

const AUMID_FMTID = '9f4c2855-9f79-4b39-a8d0-e1d42de1d5f3';
const SIG_PSDB = 0xa0000009;
const MAGIC_SPS = 0x53505331; // '1SPS'
const GUID_BYTES = Buffer.from([
  0x55, 0x28, 0x4c, 0x9f, 0x79, 0x9f, 0x39, 0x4b,
  0xa8, 0xd0, 0xe1, 0xd4, 0x2d, 0xe1, 0xd5, 0xf3
]);

function utf16Buf(str) { const b = Buffer.alloc(str.length * 2); b.write(str, 'ucs2'); return b; }

// 构造 AUMID store：[StoreSize]['1SPS'][fmtid][entrySize][pid=5][0x00][VT=31][len][string][pad]
function buildAumidStore(aumid) {
  const str = aumid + '\0';
  const strBytes = utf16Buf(str);
  // entrySize: 13 + len*2 为基础，pad 使 esz ≡ 1 (mod 4) 且 pad >= 4（与系统写入样例一致）
  let esz = 13 + strBytes.length;
  while (((4 + esz) % 4) !== 1 || esz - 13 - strBytes.length < 4) esz++;
  const storeSize = 24 + 4 + esz;
  const store = Buffer.alloc(storeSize);
  store.writeUInt32LE(storeSize, 0);
  store.writeUInt32LE(MAGIC_SPS, 4);
  GUID_BYTES.copy(store, 8);
  store.writeUInt32LE(esz, 24);
  store.writeUInt32LE(5, 28);              // pid=5
  store[32] = 0;                            // flag byte
  store.writeUInt32LE(31, 33);              // VT_LPWSTR
  store.writeUInt32LE(str.length, 37);      // chars incl null
  strBytes.copy(store, 41);
  return store;
}

// 扫描 extra data 里的 block 列表；从 PSDB 或给定起点开始
function findPsdb(b) {
  for (let i = 0; i < b.length - 8; i++) {
    if (b[i + 4] === 0x09 && b[i + 5] === 0x00 && b[i + 6] === 0x00 && b[i + 7] === 0xa0) {
      const sz = b.readUInt32LE(i);
      if (sz >= 12 && sz <= 0xffff && i + sz <= b.length) return { blockStart: i, size: sz };
    }
  }
  return null;
}

// 解析 header，返回 extra data 起始偏移（失败返回 -1）
function extraDataStart(b) {
  try {
    const flags = b.readUInt32LE(20);
    let off = 76;
    if (flags & 0x1) { off += 2 + b.readUInt16LE(off); }
    if (flags & 0x2) { off += b.readUInt32LE(off + 4); }
    const skipStr = () => {
      const cc = b.readUInt16LE(off);
      off += 2 + ((flags & 0x80) ? cc * 2 : cc);
    };
    if (flags & 0x04) skipStr();
    if (flags & 0x08) skipStr();
    if (flags & 0x10) skipStr();
    if (flags & 0x20) skipStr();
    if (flags & 0x40) skipStr();
    if (off < 76 || off > b.length) return -1;
    return off;
  } catch { return -1; }
}

// 从 start 处走 block 链，返回 terminal block 的偏移（size==0 处）
function findTerminal(b, start) {
  let off = start;
  while (off + 8 <= b.length) {
    const sz = b.readUInt32LE(off);
    if (sz === 0) return off;
    if (sz < 8 || off + sz > b.length) return -1;
    off += sz;
  }
  return -1;
}

// 组装 PSDB 块：[size][sig][store1][store2...][4字节零终止符]
function buildPsdbBlock(stores) {
  const body = Buffer.concat([...stores, Buffer.alloc(4)]); // store 列表以零 dword 结尾
  const head = Buffer.alloc(8);
  head.writeUInt32LE(8 + body.length, 0);
  head.writeUInt32LE(SIG_PSDB, 4);
  return Buffer.concat([head, body]);
}

function setAumid(lnkPath, aumid) {
  const b = fs.readFileSync(lnkPath);
  const newStore = buildAumidStore(aumid);
  const psdb = findPsdb(b);
  if (psdb) {
    // 重写 PSDB：保留非 AUMID store，替换 AUMID store
    const stores = [];
    let o = psdb.blockStart + 8;
    const end = psdb.blockStart + psdb.size;
    while (o < end) {
      const ssz = b.readUInt32LE(o);
      if (ssz < 28 || o + ssz > end) break;   // 零终止符或异常，停止
      if (b.readUInt32LE(o + 4) === MAGIC_SPS &&
          b.readUInt32LE(o + 8) === 0x9f4c2855) {
        // 旧的 AUMID store，跳过
      } else {
        stores.push(b.slice(o, o + ssz));
      }
      o += ssz;
    }
    stores.push(newStore);
    const out = Buffer.concat([b.slice(0, psdb.blockStart), buildPsdbBlock(stores), b.slice(psdb.blockStart + psdb.size)]);
    fs.writeFileSync(lnkPath, out);
    console.log('ok (replaced store) ' + lnkPath);
    return true;
  }
  // 无 PSDB：插入到 terminal block 之前
  const start = extraDataStart(b);
  if (start < 0) { console.log('fail: cannot locate extra data start'); return false; }
  const term = findTerminal(b, start);
  if (term < 0) { console.log('fail: cannot locate terminal block'); return false; }
  const out = Buffer.concat([b.slice(0, term), buildPsdbBlock([newStore]), b.slice(term)]);
  fs.writeFileSync(lnkPath, out);
  console.log('ok (inserted) ' + lnkPath);
  return true;
}

function getAumid(lnkPath) {
  const b = fs.readFileSync(lnkPath);
  const psdb = findPsdb(b);
  if (!psdb) return null;
  let o = psdb.blockStart + 8;
  const end = psdb.blockStart + psdb.size;
  while (o < end) {
    const ssz = b.readUInt32LE(o);
    if (ssz < 28 || o + ssz > end) break;
    if (b.readUInt32LE(o + 4) === MAGIC_SPS && b.readUInt32LE(o + 8) === 0x9f4c2855) {
      // entry: [esz][pid][0][VT][len][string]
      const esz = b.readUInt32LE(o + 24);
      const pid = b.readUInt32LE(o + 28);
      const vt = b.readUInt32LE(o + 33);
      if (pid === 5 && vt === 31) {
        const len = b.readUInt32LE(o + 37);
        return b.slice(o + 41, o + 41 + len * 2).toString('ucs2').replace(/\0+$/, '');
      }
    }
    o += ssz;
  }
  return null;
}

const cmd = process.argv[2];
const path = process.argv[3];
if (cmd === 'set') console.log(setAumid(path, process.argv[4]) ? 'AUMID=' + process.argv[4] : 'FAILED');
else if (cmd === 'get') console.log('AUMID=' + (getAumid(path) ?? '(none)'));
else console.log('usage: node setlnkaumid.js set|get <lnk> [aumid]');
