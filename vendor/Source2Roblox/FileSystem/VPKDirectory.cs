using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;

namespace Source2Roblox.FileSystem
{
    public class ZipEntryMetadata
    {
        public string Name { get; set; } = string.Empty;
        public ushort CompressionMethod { get; set; }
        public uint CompressedSize { get; set; }
        public uint UncompressedSize { get; set; }
        public uint LocalHeaderOffset { get; set; }
    }

    public class VPKDirectory : IEnumerable<VPKEntry>
    {
        private readonly Dictionary<string, VPKEntry> Entries = new Dictionary<string, VPKEntry>(System.StringComparer.OrdinalIgnoreCase);
        public readonly ZipArchive ZipArchive;

        public readonly uint Version;
        public readonly uint DirectorySize;

        public readonly uint EmbeddedChunkSize;
        public readonly uint ChunkHashesSize;
        public readonly uint SelfHashesSize;
        public readonly uint SignatureSize;

        private void ReadFiles(BinaryReader reader, string ext, string dir)
        {
            while (true)
            {
                string fileName = reader.ReadString(null);

                if (string.IsNullOrEmpty(fileName))
                    break;

                string path = $"{fileName}.{ext}";

                if (dir.Length > 0)
                    path = $"{dir}/{path}";

                var entry = new VPKEntry(reader);
                Entries.Add(path, entry);
            }
        }

        public VPKEntry FindEntry(string path)
        {
            if (string.IsNullOrEmpty(path))
                return null;

            if (Entries.TryGetValue(path, out VPKEntry entry))
                return entry;

            string norm = path.Replace('\\', '/').TrimStart('/');
            if (Entries.TryGetValue(norm, out entry))
                return entry;

            string alt = norm.Replace('/', '\\');
            if (Entries.TryGetValue(alt, out entry))
                return entry;

            return null;
        }

        private void ReadDirectories(BinaryReader reader, string ext)
        {
            while (true)
            {
                string dir = reader.ReadString(null);

                if (dir.Length == 0)
                    break;
                else
                    dir = dir.Trim();

                ReadFiles(reader, ext, dir);
            }
        }

        public VPKDirectory(BinaryReader reader)
        {
            uint magic = reader.ReadUInt32();
            Debug.Assert(magic == 0x55AA1234, "Not a VPK file!");

            Version = reader.ReadUInt32();
            DirectorySize = reader.ReadUInt32();

            if (Version == 2)
            {
                EmbeddedChunkSize = reader.ReadUInt32();
                ChunkHashesSize = reader.ReadUInt32();
                SelfHashesSize = reader.ReadUInt32();
                SignatureSize = reader.ReadUInt32();
            }
            else
            {
                Debug.Assert(Version == 1, "Unsupported VPK version!");
            }

            while (true)
            {
                string ext = reader.ReadString(null);

                if (ext.Length == 0)
                    break;

                ReadDirectories(reader, ext);
            }
        }

        public VPKDirectory(ZipArchive archive, byte[] rawArchiveData = null)
        {
            ZipArchive = archive;
            var metadata = rawArchiveData != null 
                ? ParseZipCentralDirectory(rawArchiveData) 
                : new Dictionary<string, ZipEntryMetadata>(StringComparer.OrdinalIgnoreCase);

            if (archive != null)
            {
                foreach (var zEntry in archive.Entries)
                {
                    string norm = zEntry.FullName.Replace('\\', '/').TrimStart('/');
                    metadata.TryGetValue(norm, out var meta);

                    var vpkEntry = new VPKEntry(zEntry, meta, rawArchiveData);
                    Entries[norm] = vpkEntry;
                    string alt = norm.Replace('/', '\\');
                    if (!Entries.ContainsKey(alt))
                        Entries[alt] = vpkEntry;
                }
            }
            else if (metadata.Count > 0 && rawArchiveData != null)
            {
                foreach (var kvp in metadata)
                {
                    var meta = kvp.Value;
                    var vpkEntry = new VPKEntry(meta, rawArchiveData);
                    Entries[meta.Name] = vpkEntry;
                    string alt = meta.Name.Replace('/', '\\');
                    if (!Entries.ContainsKey(alt))
                        Entries[alt] = vpkEntry;
                }
            }
        }

        public static Dictionary<string, ZipEntryMetadata> ParseZipCentralDirectory(byte[] zipBytes)
        {
            var dict = new Dictionary<string, ZipEntryMetadata>(StringComparer.OrdinalIgnoreCase);
            if (zipBytes == null || zipBytes.Length < 22)
                return dict;

            int eocdOffset = -1;
            int maxSearch = Math.Min(zipBytes.Length - 22, 65557);
            int startSearch = zipBytes.Length - 22;

            for (int i = startSearch; i >= startSearch - maxSearch && i >= 0; i--)
            {
                if (zipBytes[i] == 0x50 && zipBytes[i + 1] == 0x4B && zipBytes[i + 2] == 0x05 && zipBytes[i + 3] == 0x06)
                {
                    eocdOffset = i;
                    break;
                }
            }

            if (eocdOffset < 0)
                return dict;

            using (var ms = new MemoryStream(zipBytes))
            using (var reader = new BinaryReader(ms))
            {
                ms.Position = eocdOffset + 10;
                ushort totalEntries = reader.ReadUInt16();
                uint cdSize = reader.ReadUInt32();
                uint cdOffset = reader.ReadUInt32();

                if (cdOffset >= zipBytes.Length)
                    return dict;

                ms.Position = cdOffset;

                for (int i = 0; i < totalEntries && ms.Position + 46 <= zipBytes.Length; i++)
                {
                    uint sig = reader.ReadUInt32();
                    if (sig != 0x02014B50)
                        break;

                    ms.Position += 6; // version made by (2), version needed (2), flags (2)
                    ushort method = reader.ReadUInt16();
                    ms.Position += 8; // time (2), date (2), crc (4)
                    uint compSize = reader.ReadUInt32();
                    uint uncompSize = reader.ReadUInt32();
                    ushort fnLen = reader.ReadUInt16();
                    ushort extraLen = reader.ReadUInt16();
                    ushort commentLen = reader.ReadUInt16();
                    ms.Position += 8; // disk start (2), int attr (2), ext attr (4)
                    uint localOffset = reader.ReadUInt32();

                    byte[] fnBytes = reader.ReadBytes(fnLen);
                    string entryName = System.Text.Encoding.UTF8.GetString(fnBytes);
                    ms.Position += extraLen + commentLen;

                    string norm = entryName.Replace('\\', '/').TrimStart('/');
                    var meta = new ZipEntryMetadata
                    {
                        Name = norm,
                        CompressionMethod = method,
                        CompressedSize = compSize,
                        UncompressedSize = uncompSize,
                        LocalHeaderOffset = localOffset
                    };

                    dict[norm] = meta;
                    string alt = norm.Replace('/', '\\');
                    if (!dict.ContainsKey(alt))
                        dict[alt] = meta;
                }
            }

            return dict;
        }

        public IEnumerator<VPKEntry> GetEnumerator()
        {
            return Entries.Values.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Entries.Values.GetEnumerator();
        }
    }
}
