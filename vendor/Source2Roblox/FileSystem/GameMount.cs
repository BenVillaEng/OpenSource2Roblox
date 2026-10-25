using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using ValveKeyValue;

namespace Source2Roblox.FileSystem
{
    public class GameMount
    {
        public readonly string GameDir;
        public readonly string GameName;

        private readonly Dictionary<string, string> Routing;
        private readonly Dictionary<string, VPKFile> Mounts;
        private readonly Dictionary<string, string> _pathToVpk = new Dictionary<string, string>();
        private readonly List<string> _vpkPriority = new List<string>();
        private readonly List<string> _mountedGameDirs = new List<string>();

        private static readonly KVSerializer GameInfoHelper = KVSerializer.Create(KVSerializationFormat.KeyValues1Text);

        public GameMount(string gameInfoPath)
        {
            if (!gameInfoPath.EndsWith("gameinfo.txt"))
                gameInfoPath += "/gameinfo.txt";

            var gameFileInfo = new FileInfo(gameInfoPath);
            var dirInfo = gameFileInfo.Directory;

            var rootDir = dirInfo.Parent;
            var gameRoot = rootDir.FullName;

            GameName = dirInfo.Name;
            GameDir = dirInfo.FullName;
            
            Mounts = new Dictionary<string, VPKFile>();
            Routing = new Dictionary<string, string>();

            KVObject gameInfo;
            Console.WriteLine($"Loading game mount {gameInfoPath}...");

            using (var infoStream = File.OpenRead(gameInfoPath))
                gameInfo = GameInfoHelper.Deserialize(infoStream);

            var fileSystem = gameInfo["FileSystem"];
            var vpkPaths = new List<string>();

            var searchPaths = fileSystem["SearchPaths"]
                as IEnumerable<KVObject>;

            foreach (var path in searchPaths)
            {
                string value = path.Value
                    .ToString()
                    .ToLowerInvariant();

                if (value.StartsWith("|all_source_engine_paths|"))
                    value = value.Replace("|all_source_engine_paths|", $"{gameRoot}/");
                else if (value.StartsWith("|gameinfo_path|"))
                    value = value.Replace("|gameinfo_path|", GameDir);
                else
                    value = $"{gameRoot}/{value}";

                if (value.EndsWith(".vpk"))
                {
                    vpkPaths.Add(value);
                    continue;
                }

                if (value.EndsWith("/*"))
                    value = value.Replace("/*", "");
                else if (value.EndsWith("."))
                    value = value.Replace(".", "");
                else
                    continue;

                if (Directory.Exists(value))
                {
                    foreach (var file in Directory.GetFiles(value))
                    {
                        if (!file.EndsWith(".vpk"))
                            continue;

                        if (value == GameDir && !file.EndsWith("_dir.vpk"))
                            continue;

                        vpkPaths.Add(file);
                    }
                }
            }

            // Dynamically mount all VPKs in the sourceengine folder if present
            string sourceEngineDir = Path.Combine(rootDir.FullName, "sourceengine");
            if (Directory.Exists(sourceEngineDir))
            {
                foreach (var file in Directory.GetFiles(sourceEngineDir, "*_dir.vpk"))
                {
                    string cleanedFile = file.Replace('\\', '/');
                    if (!vpkPaths.Contains(cleanedFile))
                    {
                        vpkPaths.Add(cleanedFile);
                    }
                }
            }

            foreach (string path in vpkPaths)
            {
                string localPath = path
                    .Replace("_dir", "")
                    .Replace(".vpk", "")
                    .Replace('\\', '/');

                var info = new FileInfo(localPath);
                string name = info.Name;

                if (Mounts.ContainsKey(name))
                    continue;

                Console.WriteLine($"\tMounting {localPath}...");
                var vpk = new VPKFile(localPath);

                if (!vpk.Mounted)
                    continue;

                Mounts.Add(name, vpk);
            }

            // Automatically mount dependencies on other Source games, addons, and workshop folders
            MountDependencies(rootDir);

            Console.WriteLine("Ready!");
        }

        private void MountDependencies(DirectoryInfo rootDir)
        {
            Console.WriteLine("Scanning game dependencies and mounts...");

            // 1. Mount garrysmod/cfg/mount.cfg if present
            string mountCfg = Path.Combine(GameDir, "cfg", "mount.cfg");
            if (File.Exists(mountCfg))
            {
                try
                {
                    Console.WriteLine($"Parsing mount.cfg: {mountCfg}");
                    using (var fs = File.OpenRead(mountCfg))
                    {
                        var kv = GameInfoHelper.Deserialize(fs);
                        foreach (var child in kv.Children)
                        {
                            string mountPath = child.Value?.ToString();
                            if (!string.IsNullOrWhiteSpace(mountPath))
                            {
                                mountPath = mountPath.Trim('\"', '\'').Replace('\\', '/');
                                MountGameDirectory(mountPath, child.Name);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Notice: mount.cfg parse info: {ex.Message}");
                }
            }

            // 2. Discover all Steam library folders across the system
            var libraryRoots = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Sibling common directory
            if (rootDir?.Parent != null && rootDir.Parent.Exists)
            {
                string commonDir = rootDir.Parent.FullName;
                string steamApps = Path.GetFullPath(Path.Combine(commonDir, ".."));
                libraryRoots.Add(steamApps);

                string vdfPath = Path.Combine(steamApps, "libraryfolders.vdf");
                if (File.Exists(vdfPath))
                {
                    ParseLibraryFolders(vdfPath, libraryRoots);
                }
            }

            // Standard fallback Steam libraries on Windows
            string progFilesX86 = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);
            if (!string.IsNullOrEmpty(progFilesX86))
            {
                string stdSteam = Path.Combine(progFilesX86, "Steam", "steamapps");
                if (Directory.Exists(stdSteam))
                {
                    libraryRoots.Add(stdSteam);
                    string vdfPath = Path.Combine(stdSteam, "libraryfolders.vdf");
                    if (File.Exists(vdfPath))
                        ParseLibraryFolders(vdfPath, libraryRoots);
                }
            }

            string[] driveLetters = { "C", "D", "E", "F", "G", "H" };
            foreach (var drive in driveLetters)
            {
                string libCandidate = $"{drive}:/SteamLibrary/steamapps";
                if (Directory.Exists(libCandidate))
                    libraryRoots.Add(libCandidate);
            }

            // Known Source games directory signatures (CS:S, TF2, HL2, Ep1, Ep2, Portal, L4D2, DoD, CS:GO)
            var knownGames = new (string Name, string Folder, string SubDir)[]
            {
                ("cstrike", "Counter-Strike Source", "cstrike"),
                ("tf", "Team Fortress 2", "tf"),
                ("hl2", "Half-Life 2", "hl2"),
                ("episodic", "Half-Life 2", "episodic"),
                ("ep2", "Half-Life 2", "ep2"),
                ("portal", "Portal", "portal"),
                ("portal2", "Portal 2", "portal2"),
                ("left4dead2", "Left 4 Dead 2", "left4dead2"),
                ("dod", "Day of Defeat Source", "dod"),
                ("csgo", "Counter-Strike Global Offensive", "csgo")
            };

            foreach (var lib in libraryRoots)
            {
                string common = Path.Combine(lib, "common");
                if (!Directory.Exists(common)) continue;

                foreach (var g in knownGames)
                {
                    string candidate = Path.Combine(common, g.Folder, g.SubDir);
                    if (Directory.Exists(candidate))
                    {
                        MountGameDirectory(candidate, g.Name);
                    }
                }
            }

            // 3. Mount Garry's Mod addons folders
            string addonsDir = Path.Combine(GameDir, "addons");
            if (Directory.Exists(addonsDir))
            {
                try
                {
                    foreach (var sub in Directory.GetDirectories(addonsDir))
                    {
                        MountGameDirectory(sub, Path.GetFileName(sub));
                    }
                }
                catch {}
            }

            // 4. Mount Garry's Mod download folder (server downloads)
            string downloadDir = Path.Combine(GameDir, "download");
            if (Directory.Exists(downloadDir))
            {
                MountGameDirectory(downloadDir, "download");
            }

            // 5. Mount Garry's Mod workshop folder
            string workshopDir = Path.Combine(GameDir, "workshop");
            if (Directory.Exists(workshopDir))
            {
                MountGameDirectory(workshopDir, "workshop");
            }

            // 6. Mount Steam workshop content for GMod (AppID 4000)
            foreach (var lib in libraryRoots)
            {
                string wsContent4000 = Path.Combine(lib, "workshop", "content", "4000");
                if (Directory.Exists(wsContent4000))
                {
                    try
                    {
                        foreach (var wsItem in Directory.GetDirectories(wsContent4000))
                        {
                            MountGameDirectory(wsItem, $"ws_{Path.GetFileName(wsItem)}");
                        }
                    }
                    catch {}
                }
            }
        }

        private static void ParseLibraryFolders(string vdfPath, HashSet<string> libraryRoots)
        {
            try
            {
                string content = File.ReadAllText(vdfPath);
                var matches = System.Text.RegularExpressions.Regex.Matches(content, "\"path\"\\s+\"([^\"]+)\"");
                foreach (System.Text.RegularExpressions.Match m in matches)
                {
                    if (m.Groups.Count > 1)
                    {
                        string p = m.Groups[1].Value.Replace("\\\\", "/").Replace('\\', '/');
                        string apps = Path.Combine(p, "steamapps");
                        if (Directory.Exists(apps))
                            libraryRoots.Add(apps);
                        else if (Directory.Exists(p))
                            libraryRoots.Add(p);
                    }
                }
            }
            catch {}
        }

        public void MountGameDirectory(string dir, string gameLabel)
        {
            if (string.IsNullOrWhiteSpace(dir) || !Directory.Exists(dir))
                return;

            string normalized = Path.GetFullPath(dir).Replace('\\', '/');
            if (normalized.Equals(GameDir, StringComparison.OrdinalIgnoreCase) || _mountedGameDirs.Contains(normalized))
                return;

            _mountedGameDirs.Add(normalized);
            Console.WriteLine($"\t[Dependency Mount] Mounted game directory '{gameLabel}': {normalized}");

            // Mount any *_dir.vpk files inside this game directory
            try
            {
                foreach (var file in Directory.GetFiles(normalized, "*_dir.vpk", SearchOption.TopDirectoryOnly))
                {
                    string localPath = file
                        .Replace("_dir", "")
                        .Replace(".vpk", "")
                        .Replace('\\', '/');

                    string vpkName = $"{gameLabel}_{Path.GetFileName(localPath)}";
                    if (Mounts.ContainsKey(vpkName) || Mounts.ContainsKey(Path.GetFileName(localPath)))
                        continue;

                    try
                    {
                        var vpk = new VPKFile(localPath);
                        if (vpk.Mounted)
                        {
                            Mounts[vpkName] = vpk;
                            Console.WriteLine($"\t\tMounted VPK '{Path.GetFileName(file)}' as '{vpkName}'");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"\t\tFailed to mount VPK {file}: {ex.Message}");
                    }
                }

                // Check custom subdirectories in dependency game
                string customDir = Path.Combine(normalized, "custom");
                if (Directory.Exists(customDir))
                {
                    foreach (var sub in Directory.GetDirectories(customDir))
                    {
                        string normSub = Path.GetFullPath(sub).Replace('\\', '/');
                        if (!_mountedGameDirs.Contains(normSub))
                            _mountedGameDirs.Add(normSub);
                    }
                }
            }
            catch {}
        }

        public void BindZipArchive(string name, ZipArchive archive)
        {
            if (Mounts.ContainsKey(name))
            {
                try { Mounts[name].Dispose(); } catch {}
                Mounts.Remove(name);
            }

            Console.WriteLine("Warming up PakFile, this may take a moment...");
            
            // Fetch a dummy entry to make the archive load.
            archive.GetEntry("");

            Console.WriteLine("PakFile mounted!");
            Mounts[name] = new VPKFile(archive);
            lock (routingLock)
            {
                _pathToVpk.Clear();
                _vpkPriority.Clear();
            }
        }

        public static void BindZipArchive(string name, ZipArchive archive, GameMount game = null)
        {
            game = game ?? Program.GameMount;
            game.BindZipArchive(name, archive);
        }

        private readonly object routingLock = new object();

        private string GetRouting(string path)
        {
            path = Program.CleanPath(path);

            lock (routingLock)
            {
                if (Routing.TryGetValue(path, out string route))
                    return route;

                if (!string.IsNullOrWhiteSpace(Program.CustomTexturesDir))
                {
                    string customPath = Path.Combine(Program.CustomTexturesDir, path);
                    if (File.Exists(customPath))
                    {
                        route = customPath;
                        Routing[path] = route;
                        return route;
                    }

                    if (path.StartsWith("materials/"))
                    {
                        string trimmed = path.Substring("materials/".Length);
                        string customPath2 = Path.Combine(Program.CustomTexturesDir, trimmed);
                        if (File.Exists(customPath2))
                        {
                            route = customPath2;
                            Routing[path] = route;
                            return route;
                        }
                    }
                }

                if (_pathToVpk.TryGetValue(path, out string cachedName))
                {
                    route = cachedName;
                }
                else
                {
                    foreach (string priorityName in _vpkPriority)
                    {
                        VPKFile vpkPri = Mounts[priorityName];
                        if (vpkPri.HasFile(path))
                        {
                            route = priorityName;
                            _pathToVpk[path] = priorityName;
                            break;
                        }
                    }

                    if (route == null)
                    {
                        foreach (string name2 in Mounts.Keys)
                        {
                            if (_vpkPriority.Contains(name2))
                                continue;

                            VPKFile vpk2 = Mounts[name2];
                            if (vpk2.HasFile(path))
                            {
                                route = name2;
                                _pathToVpk[path] = name2;
                                _vpkPriority.Insert(0, name2);
                                break;
                            }
                        }
                    }
                }

                Routing[path] = route;

                if (route == null)
                {
                    string fullPath = Path.Combine(GameDir, path);

                    if (File.Exists(fullPath))
                    {
                        route = fullPath;
                        Routing[path] = route;
                    }
                }

                if (route == null)
                {
                    foreach (string depDir in _mountedGameDirs)
                    {
                        string depPath = Path.Combine(depDir, path);
                        if (File.Exists(depPath))
                        {
                            route = depPath;
                            Routing[path] = route;
                            break;
                        }
                    }
                }
                
                return route;
            }
        }

        public bool HasFile(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            if (File.Exists(path))
                return true;

            var route = GetRouting(path);

            if (route == null)
                return false;

            if (Mounts.ContainsKey(route))
                return true;

            if (File.Exists(route))
                return true;

            return false;
        }

        public Stream OpenRead(string path)
        {
            if (File.Exists(path))
            {
                Console.WriteLine($"Opening {path}");
                return File.OpenRead(path);
            }

            var route = GetRouting(path);

            if (route == null)
                throw new FileNotFoundException($"Couldn't find file: {path}");

            Console.WriteLine($"Opening {path}");

            if (Mounts.TryGetValue(route, out VPKFile vpk))
                return vpk.OpenRead(path);

            if (File.Exists(route))
                return File.OpenRead(route);

            // Clear invalid route.
            Routing.Remove(path);
            lock (routingLock)
            {
                _pathToVpk.Remove(path);
            }

            // Try fetching again.
            return OpenRead(path);
        }

        public static bool HasFile(string path, GameMount game = null)
        {
            game = game ?? Program.GameMount;

            if (game != null)
                return game.HasFile(path);

            return File.Exists(path);
        }

        public static Stream OpenRead(string path, GameMount game = null)
        {
            game = game ?? Program.GameMount;

            if (game != null)
                return game.OpenRead(path);

            Console.WriteLine($"Opening {path}");
            return File.OpenRead(path);
        }

        public static string GetGameName(GameMount game = null)
        {
            return (game ?? Program.GameMount)?.GameName;
        }
    }
}
