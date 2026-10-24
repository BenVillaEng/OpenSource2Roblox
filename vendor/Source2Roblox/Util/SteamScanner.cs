using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Source2Roblox.Util
{
    public class GameInfo
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string GameDir { get; set; } = string.Empty;
        public string GameInfoPath { get; set; } = string.Empty;
        public List<string> Maps { get; set; } = new List<string>();
    }

    public static class SteamScanner
    {
        private static readonly string[] SourceGameHints = {
            "Half-Life 2",
            "Half-Life 2 Deathmatch",
            "Counter-Strike Source",
            "GarrysMod",
            "Portal",
            "Portal 2",
            "Team Fortress 2",
            "Left 4 Dead",
            "Left 4 Dead 2"
        };

        public static List<string> DiscoverSteamLibraries()
        {
            var libraries = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            string programFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            string programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);

            var likelyRoots = new List<string>();
            if (!string.IsNullOrEmpty(programFilesX86)) likelyRoots.Add(Path.Combine(programFilesX86, "Steam"));
            if (!string.IsNullOrEmpty(programFiles)) likelyRoots.Add(Path.Combine(programFiles, "Steam"));
            likelyRoots.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Steam"));

            foreach (var root in likelyRoots)
            {
                if (!Directory.Exists(root)) continue;
                libraries.Add(root);

                string vdfPath = Path.Combine(root, "steamapps", "libraryfolders.vdf");
                if (File.Exists(vdfPath))
                {
                    try
                    {
                        string vdf = File.ReadAllText(vdfPath);
                        var matches = Regex.Matches(vdf, @"""path""\s+""([^""]+)""");
                        foreach (Match match in matches)
                        {
                            string path = match.Groups[1].Value.Replace(@"\\", @"\");
                            if (Directory.Exists(path))
                                libraries.Add(path);
                        }
                    }
                    catch { }
                }
            }

            return libraries.ToList();
        }

        public static List<GameInfo> ScanSourceGames()
        {
            var games = new Dictionary<string, GameInfo>(StringComparer.OrdinalIgnoreCase);

            foreach (string library in DiscoverSteamLibraries())
            {
                string commonDir = Path.Combine(library, "steamapps", "common");
                if (!Directory.Exists(commonDir)) continue;

                foreach (string title in SourceGameHints)
                {
                    string gameRoot = Path.Combine(commonDir, title);
                    if (!Directory.Exists(gameRoot)) continue;

                    try
                    {
                        foreach (string childDir in Directory.GetDirectories(gameRoot))
                        {
                            AddGame(childDir, title, games);
                        }
                    }
                    catch { }
                }

                try
                {
                    foreach (string gameRoot in Directory.GetDirectories(commonDir))
                    {
                        AddGame(gameRoot, Path.GetFileName(gameRoot), games);
                        foreach (string childDir in Directory.GetDirectories(gameRoot))
                        {
                            AddGame(childDir, Path.GetFileName(gameRoot), games);
                        }
                    }
                }
                catch { }
            }

            return games.Values.OrderBy(g => g.Name).ToList();
        }

        private static void AddGame(string gameDir, string defaultName, Dictionary<string, GameInfo> games)
        {
            string gameInfoPath = Path.Combine(gameDir, "gameinfo.txt");
            if (!File.Exists(gameInfoPath)) return;

            string id = gameDir.Replace('\\', '/').ToLowerInvariant();
            if (games.ContainsKey(id)) return;

            var gameInfo = new GameInfo
            {
                Id = id,
                Name = GetFriendlyName(gameDir, defaultName),
                GameDir = gameDir,
                GameInfoPath = gameInfoPath,
                Maps = DiscoverMaps(gameDir)
            };

            games[id] = gameInfo;
        }

        private static string GetFriendlyName(string gameDir, string defaultName)
        {
            string folder = Path.GetFileName(gameDir);
            if (SourceGameHints.Any(h => h.Equals(folder, StringComparison.OrdinalIgnoreCase)))
                return folder;

            return defaultName;
        }

        private static List<string> DiscoverMaps(string gameDir)
        {
            var maps = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            string mapsDir = Path.Combine(gameDir, "maps");
            if (Directory.Exists(mapsDir))
            {
                try
                {
                    foreach (string file in Directory.GetFiles(mapsDir, "*.bsp"))
                    {
                        maps.Add(Path.GetFileNameWithoutExtension(file));
                    }
                }
                catch { }
            }

            return maps.OrderBy(m => m).ToList();
        }
    }
}
