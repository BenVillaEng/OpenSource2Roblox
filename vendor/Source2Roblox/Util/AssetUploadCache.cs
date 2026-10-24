using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using Newtonsoft.Json;

namespace Source2Roblox.Util
{
    public static class AssetUploadCache
    {
        private static readonly string CacheFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "OpenSource2Roblox",
            "upload_cache.json"
        );

        private static Dictionary<string, string> cache = new Dictionary<string, string>();
        private static readonly object cacheLock = new object();
        private static bool loaded = false;

        public static void LoadCache()
        {
            lock (cacheLock)
            {
                if (loaded) return;
                try
                {
                    if (File.Exists(CacheFilePath))
                    {
                        string json = File.ReadAllText(CacheFilePath);
                        cache = JsonConvert.DeserializeObject<Dictionary<string, string>>(json) ?? new Dictionary<string, string>();
                    }
                }
                catch
                {
                    cache = new Dictionary<string, string>();
                }
                loaded = true;
            }
        }

        public static void SaveCache()
        {
            lock (cacheLock)
            {
                try
                {
                    string dir = Path.GetDirectoryName(CacheFilePath);
                    if (!Directory.Exists(dir))
                        Directory.CreateDirectory(dir);

                    string json = JsonConvert.SerializeObject(cache, Formatting.Indented);
                    File.WriteAllText(CacheFilePath, json);
                }
                catch
                {
                    // Ignore cache write errors
                }
            }
        }

        public static void ClearCache()
        {
            lock (cacheLock)
            {
                try
                {
                    cache.Clear();
                    if (File.Exists(CacheFilePath))
                    {
                        File.Delete(CacheFilePath);
                    }
                }
                catch
                {
                    // Ignore delete errors
                }
            }
        }

        private static string GetFileHash(string filePath)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(filePath))
                {
                    byte[] hash = md5.ComputeHash(stream);
                    return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
                }
            }
        }

        public static string GetCachedAssetId(string filePath, string assetType, string creatorType, string creatorId)
        {
            try
            {
                LoadCache();
                string hash = GetFileHash(filePath);
                string key = $"{hash}_{assetType}_{creatorType}_{creatorId}".ToLowerInvariant();

                lock (cacheLock)
                {
                    if (cache.TryGetValue(key, out string assetId))
                    {
                        return assetId;
                    }
                }
            }
            catch
            {
                // fallback to uploading
            }
            return null;
        }

        public static void SetCachedAssetId(string filePath, string assetType, string creatorType, string creatorId, string assetId)
        {
            try
            {
                LoadCache();
                string hash = GetFileHash(filePath);
                string key = $"{hash}_{assetType}_{creatorType}_{creatorId}".ToLowerInvariant();

                lock (cacheLock)
                {
                    cache[key] = assetId;
                }
                SaveCache();
            }
            catch
            {
                // ignore cache errors
            }
        }
    }
}
