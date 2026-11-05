using System;
using System.IO;
using System.IO.Compression;

namespace Source2Roblox.FileSystem
{
    public class VPKEntry
    {
        public readonly uint CRC = 0;
        public readonly ushort Index = 0;
        public readonly uint Offset = 0;
        public readonly uint Size = 0;

        public readonly int PreloadBytes;
        public readonly ZipArchiveEntry ZipEntry;
        public readonly ZipEntryMetadata Metadata;
        public readonly byte[] ArchiveData;

        public byte[] PreloadContent;

        public VPKEntry(BinaryReader reader)
        {
            CRC = reader.ReadUInt32();

            PreloadBytes = reader.ReadUInt16();
            Index = reader.ReadUInt16();

            if (Index != 0xFFFF)
            {
                Offset = reader.ReadUInt32();
                Size = reader.ReadUInt32();
            }

            PreloadContent = reader.ReadBytes(PreloadBytes);
            reader.Skip(2);
        }

        public byte[] EmbeddedContent
        {
            get
            {
                if (Index != 0x7FFF)
                    return null;

                if (PreloadContent != null)
                    return PreloadContent;

                // 1. Direct LZMA decompression if metadata indicates method 14
                if (Metadata != null && Metadata.CompressionMethod == 14 && ArchiveData != null)
                {
                    try
                    {
                        PreloadContent = DecompressLzma(ArchiveData, Metadata);
                        return PreloadContent;
                    }
                    catch (Exception ex)
                    {
                        Program.LogError($"[LZMA] Failed to decompress PakFile entry {Metadata.Name}: {ex.Message}");
                    }
                }

                // 2. Standard ZipEntry.Open() for Deflate (8) and Stored (0)
                if (ZipEntry != null)
                {
                    try
                    {
                        using (var stream = ZipEntry.Open())
                        using (var reader = new BinaryReader(stream))
                        {
                            PreloadContent = reader.ReadBytes(PreloadBytes);
                            return PreloadContent;
                        }
                    }
                    catch (NotSupportedException ex) when (ex.Message.Contains("LZMA") || ex.Message.Contains("unsupported") || (Metadata != null && Metadata.CompressionMethod == 14))
                    {
                        // Fall back to LZMA decompressor
                        byte[] archiveBytes = ArchiveData ?? TryGetArchiveBytesFromZipEntry(ZipEntry);
                        ZipEntryMetadata meta = Metadata ?? TryGetMetadataFromReflection(ZipEntry);

                        if (archiveBytes != null && meta != null)
                        {
                            try
                            {
                                PreloadContent = DecompressLzma(archiveBytes, meta);
                                return PreloadContent;
                            }
                            catch (Exception dex)
                            {
                                Program.LogError($"[LZMA] Fallback decompression failed for {ZipEntry.FullName}: {dex.Message}");
                            }
                        }

                        throw;
                    }
                }

                // 3. Fallback extraction from raw archive data if ZipEntry is null
                if (ArchiveData != null && Metadata != null)
                {
                    PreloadContent = ExtractRawZipEntry(ArchiveData, Metadata);
                    return PreloadContent;
                }

                return null;
            }
        }

        public VPKEntry(ZipArchiveEntry entry, ZipEntryMetadata meta = null, byte[] rawArchiveData = null)
        {
            Index = 0x7FFF;
            ZipEntry = entry;
            Metadata = meta;
            ArchiveData = rawArchiveData;
            PreloadBytes = (int)entry.Length;
        }

        public VPKEntry(ZipEntryMetadata meta, byte[] rawArchiveData)
        {
            Index = 0x7FFF;
            Metadata = meta;
            ArchiveData = rawArchiveData;
            PreloadBytes = (int)meta.UncompressedSize;
        }

        private static byte[] DecompressLzma(byte[] archiveData, ZipEntryMetadata meta)
        {
            using (var ms = new MemoryStream(archiveData))
            using (var reader = new BinaryReader(ms))
            {
                ms.Position = meta.LocalHeaderOffset;
                uint sig = reader.ReadUInt32();
                if (sig != 0x04034B50)
                    throw new InvalidDataException($"Invalid ZIP local file header signature 0x{sig:X8} at offset {meta.LocalHeaderOffset}");

                ms.Position = meta.LocalHeaderOffset + 26;
                ushort fnLen = reader.ReadUInt16();
                ushort extraLen = reader.ReadUInt16();

                long dataOffset = meta.LocalHeaderOffset + 30 + fnLen + extraLen;
                ms.Position = dataOffset;

                // PKWARE APPNOTE 5.8:
                // 2 bytes: LZMA version (major, minor)
                // 2 bytes: Properties size (typically 5)
                // propsSize bytes: LZMA coder properties (5 bytes)
                // remainder: raw LZMA compressed bitstream
                byte major = reader.ReadByte();
                byte minor = reader.ReadByte();
                ushort propsSize = reader.ReadUInt16();
                byte[] props = reader.ReadBytes(propsSize);

                int headerSize = 4 + propsSize;
                int rawCompressedSize = (int)meta.CompressedSize - headerSize;
                if (rawCompressedSize < 0)
                    throw new InvalidDataException("Invalid LZMA compressed size in ZIP entry header.");

                byte[] compressedData = reader.ReadBytes(rawCompressedSize);
                return LZMA.Decompress(compressedData, props, meta.UncompressedSize);
            }
        }

        private static byte[] ExtractRawZipEntry(byte[] archiveData, ZipEntryMetadata meta)
        {
            if (meta.CompressionMethod == 14)
                return DecompressLzma(archiveData, meta);

            using (var ms = new MemoryStream(archiveData))
            using (var reader = new BinaryReader(ms))
            {
                ms.Position = meta.LocalHeaderOffset + 26;
                ushort fnLen = reader.ReadUInt16();
                ushort extraLen = reader.ReadUInt16();
                ms.Position = meta.LocalHeaderOffset + 30 + fnLen + extraLen;

                byte[] compressed = reader.ReadBytes((int)meta.CompressedSize);
                if (meta.CompressionMethod == 0) // Stored
                    return compressed;

                if (meta.CompressionMethod == 8) // Deflate
                {
                    using (var compMs = new MemoryStream(compressed))
                    using (var ds = new DeflateStream(compMs, CompressionMode.Decompress))
                    using (var outMs = new MemoryStream((int)meta.UncompressedSize))
                    {
                        ds.CopyTo(outMs);
                        return outMs.ToArray();
                    }
                }

                throw new NotSupportedException($"Unsupported ZIP compression method: {meta.CompressionMethod}");
            }
        }

        private static ZipEntryMetadata TryGetMetadataFromReflection(ZipArchiveEntry entry)
        {
            try
            {
                var entryType = entry.GetType();
                var offsetField = entryType.GetField("_offsetOfLocalHeader", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                var methodField = entryType.GetField("_storedCompressionMethod", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (offsetField != null)
                {
                    long offset = (long)offsetField.GetValue(entry);
                    ushort method = 0;
                    if (methodField != null)
                    {
                        var val = methodField.GetValue(entry);
                        method = Convert.ToUInt16(val);
                    }

                    return new ZipEntryMetadata
                    {
                        Name = entry.FullName,
                        CompressionMethod = method,
                        CompressedSize = (uint)entry.CompressedLength,
                        UncompressedSize = (uint)entry.Length,
                        LocalHeaderOffset = (uint)offset
                    };
                }
            }
            catch {}
            return null;
        }

        private static byte[] TryGetArchiveBytesFromZipEntry(ZipArchiveEntry entry)
        {
            try
            {
                var archive = entry.Archive;
                var streamField = archive.GetType().GetField("_archiveStream", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (streamField != null)
                {
                    var stream = streamField.GetValue(archive) as Stream;
                    if (stream is MemoryStream ms)
                        return ms.ToArray();
                }
            }
            catch {}
            return null;
        }
    }
}
