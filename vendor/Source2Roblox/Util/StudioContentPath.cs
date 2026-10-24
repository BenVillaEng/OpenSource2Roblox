using System;
using System.IO;
using System.Linq;

namespace Source2Roblox.Util
{
    /// <summary>
    /// Resolves the Roblox Studio content directory so that rbxasset://
    /// URIs embedded in exported place files resolve correctly when Studio opens them.
    /// </summary>
    public static class StudioContentPath
    {
        private static string _cachedContentDir;

        /// <summary>
        /// Returns the path to the active Roblox Studio version's content directory,
        /// i.e. the directory that rbxasset:// maps to.
        ///
        /// Search order:
        ///   1. %localappdata%\Roblox\Versions\version-*\content  (standard Roblox install)
        ///   2. %localappdata%\Roblox Studio\Versions\version-*\content  (some mod-manager setups)
        ///   3. Fallback: %localappdata%\Roblox\content
        /// </summary>
        public static string Get()
        {
            if (_cachedContentDir != null)
                return _cachedContentDir;

            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

            string[] versionRoots = new[]
            {
                Path.Combine(localAppData, "Roblox", "Versions"),
                Path.Combine(localAppData, "Roblox Studio", "Versions"),
            };

            foreach (string versionsDir in versionRoots)
            {
                if (!Directory.Exists(versionsDir))
                    continue;

                // Pick the version directory that contains RobloxStudioBeta.exe,
                // preferring the most recently written one.
                var studioVersion = Directory
                    .GetDirectories(versionsDir, "version-*")
                    .Where(d => File.Exists(Path.Combine(d, "RobloxStudioBeta.exe")))
                    .OrderByDescending(d => new DirectoryInfo(d).LastWriteTime)
                    .FirstOrDefault();

                if (studioVersion == null)
                    continue;

                string content = Path.Combine(studioVersion, "content");
                if (Directory.Exists(content))
                {
                    _cachedContentDir = content;
                    return content;
                }
            }

            // Fallback: use the old convention path (may not resolve correctly in Studio,
            // but avoids a hard crash if Studio is not installed).
            string fallback = Path.Combine(localAppData, "Roblox", "content");
            _cachedContentDir = fallback;
            return fallback;
        }

        /// <summary>Invalidate cached path (needed between conversion runs).</summary>
        public static void Reset() => _cachedContentDir = null;
    }
}
