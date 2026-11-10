using System;
using System.Buffers.Binary;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace AppGroup {
    /// <summary>
    /// 纯二进制读写 .lnk 的 PropertyStoreDataBlock（MS-SHLLINK），
    /// 写入/替换 System.AppUserModel.ID (FMTID 9F4C2855-…, pid=5)。
    /// 不依赖 COM 属性系统——本机 SHGetPropertyStoreFromParsingName / IShellItem2
    /// 全线 E_NOINTERFACE（属性处理器通路损坏），但 Explorer 读取的是同一二进制块，
    /// 手写块与系统写入的格式已通过 Shell.Application ExtendedProperty 验证一致。
    /// 移植自 tools/setlnkaumid.js（已实测 10 个系统样例格式吻合）。
    /// </summary>
    internal static class LnkAumid {
        private const uint SIG_PSDB = 0xA0000009;
        private const uint MAGIC_SPS = 0x53505331; // '1SPS'
        private static readonly uint[] FmtidLe = { 0x9F4C2855, 0x9F794B39, 0xE1D4A8D0, 0xF3D5E12D };
        // GUID 9F4C2855-9F79-4B39-A8D0-E1D42DE1D5F3 的小端 16 字节
        private static readonly byte[] FmtidBytes = {
            0x55, 0x28, 0x4C, 0x9F, 0x79, 0x9F, 0x39, 0x4B,
            0xA8, 0xD0, 0xE1, 0xD4, 0x2D, 0xE1, 0xD5, 0xF3
        };

        /// <summary>读取 .lnk 里的 System.AppUserModel.ID；未设置返回 null。</summary>
        public static string Get(string lnkPath) {
            try {
                if (string.IsNullOrEmpty(lnkPath) || !File.Exists(lnkPath)) return null;
                byte[] b = File.ReadAllBytes(lnkPath);
                if (!FindPsdb(b, out int blockStart, out int blockSize)) return null;
                int o = blockStart + 8;
                int end = blockStart + blockSize;
                while (o + 28 <= end) {
                    uint ssz = BitConverter.ToUInt32(b, o);
                    if (ssz < 28 || o + ssz > end) break; // 零终止符或异常
                    if (BitConverter.ToUInt32(b, o + 4) == MAGIC_SPS && IsAumidFmtid(b, o + 8)) {
                        uint esz = BitConverter.ToUInt32(b, o + 24);
                        uint pid = BitConverter.ToUInt32(b, o + 28);
                        uint vt = BitConverter.ToUInt32(b, o + 33);
                        if (pid == 5 && vt == 31 && o + 41 <= end) {
                            uint chars = BitConverter.ToUInt32(b, o + 37);
                            int maxBytes = (int)Math.Min(chars * 2, (uint)(end - (o + 41)));
                            string s = Encoding.Unicode.GetString(b, o + 41, maxBytes);
                            return s.TrimEnd('\0');
                        }
                    }
                    o += (int)ssz;
                }
                return null;
            }
            catch (Exception ex) { Debug.WriteLine($"[LnkAumid] Get failed {lnkPath}: {ex.Message}"); return null; }
        }

        /// <summary>写入（或替换）.lnk 里的 System.AppUserModel.ID。已相同返回 true 且不写盘。</summary>
        public static bool Set(string lnkPath, string aumid) {
            try {
                if (string.IsNullOrEmpty(lnkPath) || string.IsNullOrEmpty(aumid) || !File.Exists(lnkPath)) return false;
                if (Get(lnkPath) == aumid) return true;

                byte[] b = File.ReadAllBytes(lnkPath);
                byte[] newStore = BuildAumidStore(aumid);

                byte[] newBlock;
                int replaceFrom, replaceLen; // 旧 PSDB 块范围（无则插入到 terminal 前）
                if (FindPsdb(b, out int blockStart, out int blockSize)) {
                    // 保留非 AUMID 的 store
                    using var bodyMs = new MemoryStream();
                    int o = blockStart + 8;
                    int end = blockStart + blockSize;
                    bool parseOk = true;
                    while (o + 28 <= end) {
                        uint ssz = BitConverter.ToUInt32(b, o);
                        if (ssz < 28 || o + ssz > end) break; // 零终止符（4 字节零）到达
                        bool isAumid = BitConverter.ToUInt32(b, o + 4) == MAGIC_SPS && IsAumidFmtid(b, o + 8);
                        if (!isAumid) bodyMs.Write(b, o, (int)ssz);
                        o += (int)ssz;
                        if (o > end) { parseOk = false; break; }
                    }
                    if (!parseOk) return false;
                    bodyMs.Write(newStore, 0, newStore.Length);
                    bodyMs.Write(new byte[4], 0, 4); // store 列表以零 dword 终止
                    newBlock = BuildBlockHeader(SIG_PSDB, bodyMs.ToArray());
                    replaceFrom = blockStart;
                    replaceLen = blockSize;
                }
                else {
                    int start = ExtraDataStart(b);
                    if (start < 0) return false;
                    int term = FindTerminal(b, start);
                    if (term < 0) return false;
                    using var bodyMs = new MemoryStream();
                    bodyMs.Write(newStore, 0, newStore.Length);
                    bodyMs.Write(new byte[4], 0, 4);
                    newBlock = BuildBlockHeader(SIG_PSDB, bodyMs.ToArray());
                    replaceFrom = term;
                    replaceLen = 0;
                }

                using var outMs = new MemoryStream(b.Length + newBlock.Length);
                outMs.Write(b, 0, replaceFrom);
                outMs.Write(newBlock, 0, newBlock.Length);
                outMs.Write(b, replaceFrom + replaceLen, b.Length - replaceFrom - replaceLen);
                File.WriteAllBytes(lnkPath, outMs.ToArray());
                return Get(lnkPath) == aumid;
            }
            catch (Exception ex) { Debug.WriteLine($"[LnkAumid] Set failed {lnkPath}: {ex.Message}"); return false; }
        }

        private static bool IsAumidFmtid(byte[] b, int off) {
            if (off + 16 > b.Length) return false;
            for (int i = 0; i < 16; i++)
                if (b[off + i] != FmtidBytes[i]) return false;
            return true;
        }

        /// <summary>定位 PropertyStoreDataBlock：sig=0xA0000009 的 extra data block。</summary>
        private static bool FindPsdb(byte[] b, out int blockStart, out int blockSize) {
            blockStart = -1; blockSize = 0;
            for (int i = 0; i + 8 <= b.Length; i++) {
                if (b[i + 4] == 0x09 && b[i + 5] == 0x00 && b[i + 6] == 0x00 && b[i + 7] == 0xA0) {
                    uint sz = BitConverter.ToUInt32(b, i);
                    if (sz >= 12 && sz <= 0xFFFF && i + sz <= b.Length) {
                        blockStart = i; blockSize = (int)sz;
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>构造 AUMID store：[StoreSize]['1SPS'][fmtid][entrySize][pid=5][0][VT=31][len][UTF16][pad]</summary>
        private static byte[] BuildAumidStore(string aumid) {
            string str = aumid + "\0";
            byte[] strBytes = Encoding.Unicode.GetBytes(str);
            int baseSize = 13 + strBytes.Length;
            // entrySize pad：使 (4+esz) % 4 == 1 且尾 pad >= 4（与系统写入样例一致）
            int esz = baseSize;
            while (((4 + esz) % 4) != 1 || esz - baseSize < 4) esz++;
            int storeSize = 24 + 4 + esz;

            byte[] store = new byte[storeSize];
            BinaryPrimitives.WriteUInt32LittleEndian(store.AsSpan(0), (uint)storeSize);
            BinaryPrimitives.WriteUInt32LittleEndian(store.AsSpan(4), MAGIC_SPS);
            Buffer.BlockCopy(FmtidBytes, 0, store, 8, 16);
            BinaryPrimitives.WriteUInt32LittleEndian(store.AsSpan(24), (uint)esz);
            BinaryPrimitives.WriteUInt32LittleEndian(store.AsSpan(28), 5);  // pid=5
            store[32] = 0;                                                   // flag byte
            BinaryPrimitives.WriteUInt32LittleEndian(store.AsSpan(33), 31);  // VT_LPWSTR
            BinaryPrimitives.WriteUInt32LittleEndian(store.AsSpan(37), (uint)str.Length); // chars incl null
            Buffer.BlockCopy(strBytes, 0, store, 41, strBytes.Length);
            return store;
        }

        private static byte[] BuildBlockHeader(uint sig, byte[] body) {
            byte[] block = new byte[8 + body.Length];
            BinaryPrimitives.WriteUInt32LittleEndian(block.AsSpan(0), (uint)block.Length);
            BinaryPrimitives.WriteUInt32LittleEndian(block.AsSpan(4), sig);
            Buffer.BlockCopy(body, 0, block, 8, body.Length);
            return block;
        }

        /// <summary>解析 .lnk header，返回 extra data 起始偏移（失败 -1）。</summary>
        private static int ExtraDataStart(byte[] b) {
            try {
                uint flags = BitConverter.ToUInt32(b, 20);
                int off = 76;
                if ((flags & 0x1) != 0) off += 2 + BitConverter.ToUInt16(b, off);      // LinkTargetIDList
                if ((flags & 0x2) != 0) off += (int)BitConverter.ToUInt32(b, off + 4); // LinkInfo
                void SkipString() {
                    ushort cc = BitConverter.ToUInt16(b, off);
                    off += 2 + ((flags & 0x80) != 0 ? cc * 2 : cc);
                }
                if ((flags & 0x04) != 0) SkipString();
                if ((flags & 0x08) != 0) SkipString();
                if ((flags & 0x10) != 0) SkipString();
                if ((flags & 0x20) != 0) SkipString();
                if ((flags & 0x40) != 0) SkipString();
                return (off >= 76 && off <= b.Length) ? off : -1;
            }
            catch { return -1; }
        }

        /// <summary>从 extra data 起点走 block 链，返回 terminal block 偏移（size==0 处）。</summary>
        private static int FindTerminal(byte[] b, int start) {
            int off = start;
            while (off + 8 <= b.Length) {
                uint sz = BitConverter.ToUInt32(b, off);
                if (sz == 0) return off;
                if (sz < 8 || off + sz > b.Length) return -1;
                off += (int)sz;
            }
            return -1;
        }
    }
}
