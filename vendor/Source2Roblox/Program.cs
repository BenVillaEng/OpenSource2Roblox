using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Source2Roblox.FileSystem;
using Source2Roblox.Geometry;
using Source2Roblox.Models;
using Source2Roblox.Textures;
using Source2Roblox.Util;
using Source2Roblox.World;

namespace Source2Roblox
{
    public class Program
    {
        private static readonly Dictionary<string, string> argMap = new Dictionary<string, string>();
        public static GameMount GameMount { get; private set; }

        public const int STUDS_TO_VMF = 12;
        public static bool LOCAL_ONLY = true;
        public static bool NO_PROMPT = false;
        public static bool UploadAssets = false;
        public static bool UploadMeshes = false;
        public static string RobloxApiKey = "";
        public static string RobloxCreatorType = "user";
        public static string RobloxCreatorId = "";
        public static string CustomTexturesDir = "";

        public static bool HasRobloxUploadCredentials =>
            !string.IsNullOrWhiteSpace(RobloxApiKey) &&
            !string.IsNullOrWhiteSpace(RobloxCreatorId) &&
            (RobloxCreatorType == "user" || RobloxCreatorType == "group");
        
        public static string GetArg(string argName)
        {
            if (argMap.TryGetValue(argName, out string arg))
                return arg;

            return null;
        }

        public static string CleanPath(string path)
        {
            string cleaned = path
                .ToLowerInvariant()
                .Replace('\\', '/')
                .Replace("//", "/");

            return cleaned;
        }

        public static void LogError(string log)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine(log);
            Console.ForegroundColor = ConsoleColor.Gray;
        }

        public static event Action<string, string, object> OnEmit;
        public static readonly HashSet<string> ExcludedTextures = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public static void Emit(string type, string message, object data = null)
        {
            var payload = new
            {
                type,
                message,
                data,
                timestamp = DateTimeOffset.UtcNow.ToString("o")
            };

            Console.WriteLine("S2R_EVENT " + JsonConvert.SerializeObject(payload));
            OnEmit?.Invoke(type, message, data);
        }

        private static string GetDefaultOutputRoot()
        {
            string documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            return Path.Combine(documents, "Roblox Studio", "Source2Roblox Exports");
        }

        private static string RequireGameInfo(string gameDir)
        {
            if (string.IsNullOrWhiteSpace(gameDir))
                throw new ArgumentException("Missing required -game path.");

            string gameInfo = gameDir.EndsWith("gameinfo.txt", StringComparison.OrdinalIgnoreCase)
                ? gameDir
                : Path.Combine(gameDir, "gameinfo.txt");

            if (!File.Exists(gameInfo))
            {
                try
                {
                    string dir = Path.GetDirectoryName(gameInfo) ?? gameDir;
                    string gameName = new DirectoryInfo(dir).Name;
                    string fallback = $"\"GameInfo\"\n{{\n\tgame \"{gameName}\"\n\ttitle \"{gameName}\"\n\tFileSystem\n\t{{\n\t\tSteamAppId 4000\n\t\tSearchPaths\n\t\t{{\n\t\t\tgame+mod |gameinfo_path|.\n\t\t}}\n\t}}\n}}\n";
                    File.WriteAllText(gameInfo, fallback);
                }
                catch
                {
                    throw new FileNotFoundException("The selected game folder does not contain gameinfo.txt and one could not be generated.", gameInfo);
                }
            }

            return gameDir;
        }

        private static string ReadJobArgs(string[] args)
        {
            string jobPath = null;

            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == "-job")
                {
                    jobPath = args[i + 1];
                    break;
                }
            }

            if (string.IsNullOrWhiteSpace(jobPath))
                return null;

            if (!File.Exists(jobPath))
                throw new FileNotFoundException("Job file was not found.", jobPath);

            var job = JsonConvert.DeserializeObject<Dictionary<string, string>>(File.ReadAllText(jobPath));
            var expanded = new List<string>();

            foreach (var pair in job)
            {
                if (string.IsNullOrWhiteSpace(pair.Value))
                    continue;

                expanded.Add("-" + pair.Key.TrimStart('-'));
                expanded.Add(pair.Value);
            }

            return string.Join("\n", expanded);
        }

        [STAThread]
        public static int Main(string[] args)
        {
            if (args.Length == 0)
            {
                var app = new App();
                app.InitializeComponent();
                app.Run();
                return 0;
            }

            try
            {
                string jobArgs = ReadJobArgs(args);

                if (jobArgs != null)
                    args = jobArgs.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
            }
            catch (Exception e)
            {
                Emit("error", e.Message, new { detail = e.ToString() });
                return 1;
            }

            argMap.Clear();
            LOCAL_ONLY = true;
            UploadAssets = false;
            UploadMeshes = false;
            NO_PROMPT = false;
            RobloxApiKey = "";
            RobloxCreatorType = "user";
            RobloxCreatorId = "";
            CustomTexturesDir = "";
            GameMount = null;
            Textures.ValveMaterial.ClearCache();
            Util.StudioContentPath.Reset();
            Util.RobloxAssetResolver.ResetCounters();

            string argKey = "";

            foreach (string arg in args)
            {
                if (arg.StartsWith("-"))
                {
                    if (!string.IsNullOrEmpty(argKey))
                        argMap[argKey] = "";

                    argKey = arg;
                }
                else if (!string.IsNullOrEmpty(argKey))
                {
                    argMap[argKey] = arg;
                    argKey = "";
                }
            }

            if (!string.IsNullOrEmpty(argKey))
                argMap[argKey] = "";

            string noPrompt = GetArg("-noPrompt");
            string upload   = GetArg("-upload");
            string uploadMeshes = GetArg("-uploadMeshes");

            string gameDir = GetArg("-game");
            string model = GetArg("-model");
            string mesh = GetArg("-mesh");

            string mapName = GetArg("-map");
            string inspectMap = GetArg("-inspect");
            string previewTexture = GetArg("-previewTexture");
            string excludeList = GetArg("-excludeList");
            string excludeTextures = GetArg("-excludeTextures");
            string vtfName = GetArg("-vtf");
            string outputRoot = GetArg("-output");
            RobloxApiKey = GetArg("-robloxApiKey") ?? Environment.GetEnvironmentVariable("S2R_ROBLOX_API_KEY") ?? "";
            RobloxCreatorType = (GetArg("-robloxCreatorType") ?? "user").ToLowerInvariant();
            RobloxCreatorId = GetArg("-robloxCreatorId") ?? "";
            CustomTexturesDir = GetArg("-customTexturesDir") ?? "";

            string clearCache = GetArg("-clearCache");
            if (clearCache != null)
            {
                try
                {
                    Source2Roblox.Util.AssetUploadCache.ClearCache();
                    Emit("raw", "Upload cache cleared successfully.");
                    return 0;
                }
                catch (Exception e)
                {
                    Emit("error", $"Failed to clear upload cache: {e.Message}");
                    return 1;
                }
            }

            string verifyCreds = GetArg("-verifyCredentials");
            if (verifyCreds != null)
            {
                var result = Util.RobloxAssetResolver.VerifyCredentialsAsync(RobloxApiKey, RobloxCreatorType, RobloxCreatorId).GetAwaiter().GetResult();
                if (result.Success)
                {
                    Emit("credential_check", result.Message, new { valid = true, creatorType = RobloxCreatorType, creatorId = RobloxCreatorId });
                    return 0;
                }
                else
                {
                    Emit("credential_check", result.Message, new { valid = false, error = result.Message });
                    return 1;
                }
            }

            bool hasGame = !string.IsNullOrWhiteSpace(gameDir);
            bool vtfNeedsGame = vtfName != null && !Path.IsPathRooted(vtfName);

            if (!hasGame && (mapName != null || model != null || vtfNeedsGame || inspectMap != null || previewTexture != null))
            {
                Emit("error", "A Source game folder is required for maps, models, textures, and previews. Use an absolute VTF file path for texture-only export.");
                return 1;
            }

            if (upload != null)
            {
                LOCAL_ONLY = false;
                UploadAssets = true;
            }

            if (uploadMeshes != null)
                UploadMeshes = true;

            if (noPrompt != null)
                NO_PROMPT = true;

            try
            {
                outputRoot = string.IsNullOrWhiteSpace(outputRoot) ? GetDefaultOutputRoot() : outputRoot;
                Directory.CreateDirectory(outputRoot);

                if (hasGame)
                    gameDir = RequireGameInfo(gameDir);

                if (hasGame)
                    GameMount = new GameMount(gameDir);

                if (!string.IsNullOrWhiteSpace(excludeList) && File.Exists(excludeList))
                {
                    try
                    {
                        var lines = File.ReadAllLines(excludeList);
                        foreach (var line in lines)
                        {
                            string trimmed = line.Trim();
                            if (!string.IsNullOrWhiteSpace(trimmed))
                                ExcludedTextures.Add(trimmed);
                        }
                        Emit("info", $"Loaded {ExcludedTextures.Count} excluded texture filter rules.");
                    }
                    catch (Exception ex)
                    {
                        Emit("warning", $"Failed to read excludeList file '{excludeList}': {ex.Message}");
                    }
                }

                if (!string.IsNullOrWhiteSpace(excludeTextures))
                {
                    var parts = excludeTextures.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var part in parts)
                    {
                        string trimmed = part.Trim();
                        if (!string.IsNullOrWhiteSpace(trimmed))
                            ExcludedTextures.Add(trimmed);
                    }
                }

                if (!string.IsNullOrWhiteSpace(previewTexture))
                {
                    bool ok = GenerateTexturePreview(previewTexture, outputRoot);
                    return ok ? 0 : 1;
                }

                if (!string.IsNullOrWhiteSpace(inspectMap))
                {
                    bool ok = InspectMap(inspectMap);
                    return ok ? 0 : 1;
                }

                Emit("start", "Starting Source2Roblox conversion.", new
                {
                    game = gameDir,
                    output = outputRoot,
                    map = mapName,
                    model = model,
                    vtf = vtfName
                });
            }
            catch (Exception e)
            {
                Emit("error", e.Message, new { detail = e.ToString() });
                return 1;
            }

            var tasks = new List<Task>();
            bool hasErrors = false;

            RobloxAssetResolver.ResetModerationState();
            RobloxAssetResolver.ResetCounters();

            if (vtfName != null)
            {
                var vtfNames = vtfName.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string rawVtf in vtfNames)
                {
                    string singleVtfName = rawVtf.Trim();
                    tasks.Add(Task.Run(() =>
                    {
                        try
                        {
                            ConvertVTF(singleVtfName, outputRoot);
                        }
                        catch (Exception e)
                        {
                            hasErrors = true;
                            if (RobloxAssetResolver.IsModerated || e is RobloxModerationException || e.InnerException is RobloxModerationException)
                            {
                                Emit("moderation_blocked", "VTF conversion paused: Roblox account is moderated.", new
                                {
                                    asset = RobloxAssetResolver.FlaggedAssetPath,
                                    reason = RobloxAssetResolver.ModerationReason,
                                    canResume = true
                                });
                            }
                            else
                            {
                                Emit("error", $"VTF conversion failed for {singleVtfName}: {e.Message}", new { detail = e.ToString() });
                            }
                        }
                    }));
                }
            }

            if (model != null)
            {
                var models = model.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string rawModel in models)
                {
                    string singleModel = rawModel.Trim();
                    tasks.Add(Task.Run(() =>
                    {
                        try
                        {
                            ConvertModel(singleModel, outputRoot);
                        }
                        catch (Exception e)
                        {
                            hasErrors = true;
                            if (RobloxAssetResolver.IsModerated || e is RobloxModerationException || e.InnerException is RobloxModerationException)
                            {
                                Emit("moderation_blocked", "Model conversion paused: Roblox account is moderated.", new
                                {
                                    asset = RobloxAssetResolver.FlaggedAssetPath,
                                    reason = RobloxAssetResolver.ModerationReason,
                                    canResume = true
                                });
                            }
                            else
                            {
                                Emit("error", $"Model conversion failed for {singleModel}: {e.Message}", new { detail = e.ToString() });
                            }
                        }
                    }));
                }
            }

            if (mapName != null)
            {
                var maps = mapName.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string rawMap in maps)
                {
                    string singleMap = rawMap.Trim();
                    tasks.Add(Task.Run(() =>
                    {
                        try
                        {
                            ConvertMap(singleMap, outputRoot);
                        }
                        catch (Exception e)
                        {
                            hasErrors = true;
                            if (RobloxAssetResolver.IsModerated || e is RobloxModerationException || e.InnerException is RobloxModerationException)
                            {
                                Emit("moderation_blocked", "Map conversion paused: Roblox account is moderated.", new
                                {
                                    asset = RobloxAssetResolver.FlaggedAssetPath,
                                    reason = RobloxAssetResolver.ModerationReason,
                                    canResume = true
                                });
                            }
                            else
                            {
                                Emit("error", $"Map conversion failed for {singleMap}: {e.Message}", new { detail = e.ToString() });
                            }
                        }
                    }));
                }
            }

            if (tasks.Count > 0)
            {
                Task.WhenAll(tasks).GetAwaiter().GetResult();
            }

            if (hasErrors)
            {
                if (RobloxAssetResolver.IsModerated)
                {
                    Emit("moderation_blocked", "Conversion halted due to Roblox account moderation. Ready to resume.", new
                    {
                        canResume = true,
                        asset = RobloxAssetResolver.FlaggedAssetPath,
                        reason = RobloxAssetResolver.ModerationReason
                    });
                    return 2;
                }

                Emit("error", "One or more conversion tasks encountered errors. Place file was not generated.");
                return 1;
            }

            Emit("done", "Conversion finished.");
            return 0;
        }

        private static void ConvertVTF(string vtfName, string outputRoot)
        {
            var info = new FileInfo(vtfName);
            string name = info.Name.Replace(".vtf", "");

            string dir = Path.Combine(outputRoot, "ExamineVTF", name);
            Directory.CreateDirectory(dir);

            Emit("progress", $"Exporting VTF {vtfName}.", new { output = dir });

            using (var stream = Path.IsPathRooted(vtfName) ? File.OpenRead(vtfName) : GameMount.OpenRead(vtfName))
            using (var reader = new BinaryReader(stream))
            {
                var file = new VTFFile(reader, true);

                for (int i = 0; i < file.NumFrames; i++)
                {
                    var frame = file.Frames[i];

                    for (int j = 0; j < frame.Count; j++)
                    {
                        var mipmap = frame[j];

                        for (int k = 0; k < mipmap.Count; k++)
                        {
                            var image = mipmap[k];
                            string savePath = Path.Combine(dir, $"{name}_{i}_{j}_{k}.png");
                            image.Save(savePath);
                        }
                    }
                }

                var lowRes = file.LowResImage;
                var highRes = file.HighResImage;

                if (lowRes != null)
                {
                    string lowResPath = Path.Combine(dir, $"{name}_LOW_RES.png");
                    lowRes.Save(lowResPath);
                }

                var normalMap = SSBump.ToNormalMap(highRes);
                string normalPath = Path.Combine(dir, $"{name}_NORMAL_MAP.png");

                normalMap.Save(normalPath);
            }

            Emit("output", "VTF export complete.", new { path = dir });
        }

        private static void ConvertModel(string model, string outputRoot)
        {
            string exportDir = Path.Combine(outputRoot, "SourceModels");
            string robloxOutputDir = Path.Combine(outputRoot, GameMount.GameName);
            Directory.CreateDirectory(exportDir);

            if (string.IsNullOrWhiteSpace(model))
            {
                throw new ArgumentException("Interactive model search is not supported by the app wrapper. Enter a model path or name.");
            }

            Emit("progress", $"Processing model {model}.", new { output = exportDir });
            var mdl = new ModelFile(model);
            try
            {
                MeshBuilder.BakeMDL_RBXM(mdl, 0, outputRoot);
                MeshBuilder.BakeMDL_OBJ(mdl, exportDir);
                Emit("output", "Model export complete.", new { path = robloxOutputDir, objPath = exportDir });
            }
            catch (RobloxModerationException mex)
            {
                LogError("\n================================================================================");
                LogError(" [CRITICAL] ROBLOX MODERATION DETECTED: Upload stopped.");
                LogError($" Flagged Asset: {mex.AssetPath}");
                LogError($" Reason: {mex.Details}");
                LogError(" All pending uploads were stopped immediately.");
                LogError(" Previously uploaded assets were safely saved in upload_cache.json.");
                LogError(" Please review your account status on roblox.com.");
                LogError(" When ready, click 'RESUME CONVERSION'.");
                LogError("================================================================================\n");

                Emit("moderation_blocked", "Roblox account is moderated. Upload paused.", new
                {
                    asset = mex.AssetPath,
                    reason = mex.Details,
                    canResume = true
                });
                throw;
            }
        }

        public static string ResolveMapPath(string mapInput)
        {
            if (string.IsNullOrWhiteSpace(mapInput))
                return mapInput;

            string trimmed = mapInput.Trim();

            // Handle .nav files: resolve to corresponding .bsp
            if (trimmed.EndsWith(".nav", StringComparison.OrdinalIgnoreCase))
            {
                string bspCandidate = trimmed.Substring(0, trimmed.Length - 4) + ".bsp";
                if (File.Exists(bspCandidate))
                    return bspCandidate;
                trimmed = trimmed.Substring(0, trimmed.Length - 4);
            }
            else if (trimmed.EndsWith(".bsp", StringComparison.OrdinalIgnoreCase))
            {
                if (File.Exists(trimmed))
                    return trimmed;
                trimmed = trimmed.Substring(0, trimmed.Length - 4);
            }

            if (File.Exists(trimmed))
                return trimmed;

            if (File.Exists(trimmed + ".bsp"))
                return trimmed + ".bsp";

            string baseMapName = Path.GetFileName(trimmed);

            if (GameMount != null && !string.IsNullOrWhiteSpace(GameMount.GameDir))
            {
                // Check maps/
                string directMap = Path.Combine(GameMount.GameDir, "maps", $"{baseMapName}.bsp");
                if (File.Exists(directMap))
                    return directMap;

                // Check download/maps/
                string downloadMap = Path.Combine(GameMount.GameDir, "download", "maps", $"{baseMapName}.bsp");
                if (File.Exists(downloadMap))
                    return downloadMap;

                // Check root
                string rootMap = Path.Combine(GameMount.GameDir, $"{baseMapName}.bsp");
                if (File.Exists(rootMap))
                    return rootMap;

                // Check addons/*/maps and direct addon maps
                string addonsDir = Path.Combine(GameMount.GameDir, "addons");
                if (Directory.Exists(addonsDir))
                {
                    try
                    {
                        foreach (var addonDir in Directory.GetDirectories(addonsDir))
                        {
                            string addonMap = Path.Combine(addonDir, "maps", $"{baseMapName}.bsp");
                            if (File.Exists(addonMap))
                                return addonMap;

                            string directAddonMap = Path.Combine(addonDir, $"{baseMapName}.bsp");
                            if (File.Exists(directAddonMap))
                                return directAddonMap;
                        }
                    }
                    catch {}
                }

                // Check Steam workshop content folders (4000 for Garry's Mod, 550 for L4D2, etc.)
                string workshopBase = Path.GetFullPath(Path.Combine(GameMount.GameDir, "..", "..", "workshop", "content"));
                if (Directory.Exists(workshopBase))
                {
                    try
                    {
                        foreach (var appDir in Directory.GetDirectories(workshopBase))
                        {
                            foreach (var wsItem in Directory.GetDirectories(appDir))
                            {
                                string wsMap = Path.Combine(wsItem, "maps", $"{baseMapName}.bsp");
                                if (File.Exists(wsMap))
                                    return wsMap;

                                string wsDirectMap = Path.Combine(wsItem, $"{baseMapName}.bsp");
                                if (File.Exists(wsDirectMap))
                                    return wsDirectMap;
                            }
                        }
                    }
                    catch {}
                }
            }

            return $"maps/{baseMapName}.bsp";
        }

        private static void ConvertMap(string mapName, string outputRoot)
        {
            string gameName = GameMount?.GameName ?? "SourceGame";
            string exportDir = Path.Combine(outputRoot, gameName, "maps");
            Directory.CreateDirectory(exportDir);

            string bspPath = ResolveMapPath(mapName);
            string cleanName = Path.GetFileNameWithoutExtension(bspPath);

            Emit("progress", $"Processing map {cleanName} from {bspPath}.", new { output = exportDir });
            var bsp = new BSPFile(bspPath, GameMount);
            try
            {
                MeshBuilder.BakeBSP_RBXL(bsp, GameMount, outputRoot);
                Emit("output", "Map export complete.", new { path = exportDir });
            }
            catch (RobloxModerationException mex)
            {
                LogError("\n================================================================================");
                LogError(" [CRITICAL] ROBLOX MODERATION DETECTED: Upload stopped.");
                LogError($" Flagged Asset: {mex.AssetPath}");
                LogError($" Reason: {mex.Details}");
                LogError(" All pending uploads were stopped immediately to prevent asset loss or broken maps.");
                LogError(" Previously uploaded assets were safely saved in upload_cache.json.");
                LogError(" Please review your account status on roblox.com.");
                LogError(" When ready, click 'RESUME CONVERSION' to continue uploading.");
                LogError("================================================================================\n");

                Emit("moderation_blocked", "Roblox account is moderated. Upload paused.", new
                {
                    asset = mex.AssetPath,
                    reason = mex.Details,
                    canResume = true
                });
                throw;
            }
        }

        private class ModelSummary
        {
            public string Path { get; set; } = "";
            public string Type { get; set; } = "Static Prop";
            public int InstanceCount { get; set; } = 0;
            public List<int> Skins { get; set; } = new List<int>();
            public bool ExistsInGame { get; set; } = false;
        }

        private static bool InspectMap(string mapName)
        {
            try
            {
                string bspPath = ResolveMapPath(mapName);
                if (string.IsNullOrWhiteSpace(bspPath) || (!File.Exists(bspPath) && (GameMount == null || !GameMount.HasFile(bspPath, GameMount))))
                {
                    Emit("error", $"Could not find map '{mapName}'. Please verify the map exists in maps/, download/, addons/, or workshop content directories.");
                    return false;
                }

                string cleanName = Path.GetFileNameWithoutExtension(bspPath);
                Emit("progress", $"Inspecting assets inside map {cleanName} from {bspPath}...");
                var bsp = new BSPFile(bspPath, GameMount);

                var modelMap = new Dictionary<string, ModelSummary>(StringComparer.OrdinalIgnoreCase);

                // 1. Static Props from sprp GameLump
                var sprpLump = bsp.FindGameLump("sprp");
                if (sprpLump != null && sprpLump.Content != null)
                {
                    using (var propStream = new MemoryStream(sprpLump.Content))
                    using (var reader = new BinaryReader(propStream))
                    {
                        var staticProps = new World.Types.StaticProps(bsp, sprpLump, reader);
                        foreach (var prop in staticProps)
                        {
                            if (prop.PropType >= 0 && prop.PropType < staticProps.Strings.Length)
                            {
                                string mdl = staticProps.Strings[prop.PropType];
                                if (string.IsNullOrWhiteSpace(mdl)) continue;

                                if (!modelMap.TryGetValue(mdl, out var summary))
                                {
                                    summary = new ModelSummary
                                    {
                                        Path = mdl,
                                        Type = "Static Prop",
                                        ExistsInGame = GameMount.HasFile(mdl, GameMount)
                                    };
                                    modelMap[mdl] = summary;
                                }

                                summary.InstanceCount++;
                                if (!summary.Skins.Contains(prop.Skin))
                                    summary.Skins.Add(prop.Skin);
                            }
                        }
                    }
                }

                // 2. Dynamic & Physics Props from Entities
                var entityProps = bsp.FindEntitiesOfClass("prop_dynamic", "prop_physics", "prop_physics_multiplayer");
                foreach (var prop in entityProps)
                {
                    string mdl = prop.GetString("model");
                    if (string.IsNullOrWhiteSpace(mdl)) continue;

                    int skin = prop.GetInt("skin") ?? 0;

                    if (!modelMap.TryGetValue(mdl, out var summary))
                    {
                        summary = new ModelSummary
                        {
                            Path = mdl,
                            Type = prop.ClassName,
                            ExistsInGame = GameMount.HasFile(mdl, GameMount)
                        };
                        modelMap[mdl] = summary;
                    }

                    summary.InstanceCount++;
                    if (!summary.Skins.Contains(skin))
                        summary.Skins.Add(skin);
                }

                // 3. Textures & Materials
                var textureList = bsp.TexDataStringData.Values
                    .Where(t => !string.IsNullOrWhiteSpace(t))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(t => t)
                    .Select(t =>
                    {
                        var risk = AssessTextureRisk(t);
                        return new
                        {
                            path = t,
                            type = t.StartsWith("skybox/", StringComparison.OrdinalIgnoreCase) ? "Skybox"
                                 : t.StartsWith("decals/", StringComparison.OrdinalIgnoreCase) ? "Decal"
                                 : t.StartsWith("models/", StringComparison.OrdinalIgnoreCase) ? "Model Texture"
                                 : "World Texture",
                            riskLevel = risk.Level,
                            riskScore = risk.Score,
                            riskTags = risk.Tags,
                            riskReason = risk.Reason,
                            isRisky = risk.IsRisky
                        };
                    })
                    .ToList();

                // 4. Entities breakdown
                var entitySummary = bsp.Entities
                    .GroupBy(e => e.ClassName)
                    .Select(g => new { className = g.Key, count = g.Count() })
                    .OrderByDescending(g => g.count)
                    .ToList();

                var worldSpawn = bsp.FindEntityByClass("worldspawn");
                string skyName = worldSpawn?.GetString("skyname") ?? "default";

                var payload = new
                {
                    map = bsp.Name,
                    bspVersion = bsp.Version,
                    skyName,
                    summary = new
                    {
                        uniqueModels = modelMap.Count,
                        totalPropInstances = modelMap.Values.Sum(m => m.InstanceCount),
                        uniqueTextures = textureList.Count,
                        riskyTextures = textureList.Count(t => t.isRisky),
                        highRiskTextures = textureList.Count(t => t.riskLevel == "HIGH"),
                        totalFaces = bsp.Faces.Count,
                        totalDisplacements = bsp.Displacements.Count,
                        totalBrushes = bsp.Brushes.Count,
                        totalEntities = bsp.Entities.Count
                    },
                    models = modelMap.Values.OrderByDescending(m => m.InstanceCount).ToList(),
                    textures = textureList,
                    entities = entitySummary
                };

                Emit("map_inspection", $"Map inspection successful for {bsp.Name}.", payload);
                return true;
            }
            catch (Exception ex)
            {
                Emit("error", $"Failed to inspect map {mapName}: {ex.Message}", new { detail = ex.ToString() });
                return false;
            }
        }

        public class TextureRiskResult
        {
            public string Level { get; set; } = "CLEAN";
            public int Score { get; set; } = 0;
            public List<string> Tags { get; set; } = new List<string>();
            public string Reason { get; set; } = "No known risks detected.";
            public bool IsRisky => Score >= 50;
        }

        public static TextureRiskResult AssessTextureRisk(string path)
        {
            var result = new TextureRiskResult();
            if (string.IsNullOrWhiteSpace(path))
                return result;

            string lower = path.ToLowerInvariant().Replace('\\', '/');

            // 1. Critical: Nudity / Soft NSFW / Pinup / Adult Content
            string[] nudityKeywords = new[]
            {
                "pinup", "girl", "nude", "naked", "playboy", "hentai", "anime_girl", "sexy", "boob",
                "tits", "bikini", "lingerie", "swimsuit", "cleavage", "ass", "butt", "penis", "dick",
                "cock", "vagina", "pussy", "strip", "stripper", "erotic", "porn", "xxx", "waifu",
                "poster_girl", "girl_poster", "woman_poster", "babes", "adult", "bra", "panties",
                "underwear", "nsfw", "fetish", "lewd", "camgirl", "sensual", "nudity"
            };

            // 2. High: Gore / Flesh / Blood / Corpses
            string[] goreKeywords = new[]
            {
                "gore", "blood", "bloody", "guts", "flesh", "gib", "gibs", "corpse", "dead_body",
                "deadbody", "mutilat", "dismember", "severed", "wound", "entrails", "viscera",
                "slaughter", "decapitat", "splatter", "murder", "carnage", "decap", "hanging_body",
                "charred_corpse", "severed_limb"
            };

            // 3. Medium/High: Alcohol / Drugs / Tobacco / Controlled Substances
            string[] drugKeywords = new[]
            {
                "beer", "liquor", "wine", "vodka", "whiskey", "whisky", "rum", "tequila", "alcohol",
                "bar_sign", "pub_sign", "cigar", "cigarette", "smoke", "tobacco", "weed", "cannabis",
                "marijuana", "joint", "bong", "drug", "pills", "syringe", "needle", "cocaine", "meth",
                "heroin", "pharmacy", "drunk", "bottle_labels", "budweiser", "heineken", "corona",
                "jackdaniels", "ashtray", "smokes", "carton", "pack_smokes", "snuff", "hookah", "vape"
            };

            // 4. Critical: Hate Speech / Extremist
            string[] hateKeywords = new[]
            {
                "nazi", "swastika", "hitler", "ss_", "terror", "kkk", "fascist", "aryan"
            };

            var matchedReasons = new List<string>();

            foreach (var kw in nudityKeywords)
            {
                if (lower.Contains(kw))
                {
                    result.Tags.Add("Nudity/Pinup");
                    matchedReasons.Add($"Pinup/Nudity keyword ('{kw}')");
                    result.Score = Math.Max(result.Score, 95);
                    break;
                }
            }

            foreach (var kw in goreKeywords)
            {
                if (lower.Contains(kw))
                {
                    result.Tags.Add("Gore/Blood");
                    matchedReasons.Add($"Gore/Blood keyword ('{kw}')");
                    result.Score = Math.Max(result.Score, 85);
                    break;
                }
            }

            foreach (var kw in drugKeywords)
            {
                if (lower.Contains(kw))
                {
                    result.Tags.Add("Alcohol/Drugs");
                    matchedReasons.Add($"Alcohol/Drugs keyword ('{kw}')");
                    result.Score = Math.Max(result.Score, 75);
                    break;
                }
            }

            foreach (var kw in hateKeywords)
            {
                if (lower.Contains(kw))
                {
                    result.Tags.Add("Controversial");
                    matchedReasons.Add($"Extremist/Hate keyword ('{kw}')");
                    result.Score = Math.Max(result.Score, 99);
                    break;
                }
            }

            // 5. High-Risk Zones: Decals, posters, sprays, custom folders
            bool isDecalOrPoster = lower.StartsWith("decals/") || lower.Contains("/decals/") ||
                                   lower.Contains("poster") || lower.Contains("spray") ||
                                   lower.Contains("custom/") || lower.Contains("graffiti");
            if (isDecalOrPoster)
            {
                result.Tags.Add("Custom Decal/Poster");
                if (result.Score < 50)
                {
                    result.Score = 50;
                    matchedReasons.Add("Decal/Poster/Custom spray");
                }
            }

            if (result.Score >= 80)
            {
                result.Level = "HIGH";
                result.Reason = $"High risk of Roblox moderation: {string.Join(", ", matchedReasons)}. Strongly recommend deselecting.";
            }
            else if (result.Score >= 50)
            {
                result.Level = "MEDIUM";
                result.Reason = $"Elevated risk: {string.Join(", ", matchedReasons)}. Review before uploading.";
            }
            else
            {
                result.Level = "CLEAN";
                result.Reason = "Standard environmental asset. Low risk.";
            }

            return result;
        }

        private static bool GenerateTexturePreview(string texturePath, string outputRoot)
        {
            try
            {
                string matPath = texturePath.Trim().Replace('\\', '/');
                if (!matPath.EndsWith(".vmt", StringComparison.OrdinalIgnoreCase) &&
                    !matPath.EndsWith(".vtf", StringComparison.OrdinalIgnoreCase))
                {
                    matPath += ".vmt";
                }
                if (!matPath.StartsWith("materials/", StringComparison.OrdinalIgnoreCase))
                {
                    matPath = "materials/" + matPath;
                }

                string vtfPath = null;
                if (matPath.EndsWith(".vmt", StringComparison.OrdinalIgnoreCase))
                {
                    if (GameMount != null && GameMount.HasFile(matPath, GameMount))
                    {
                        var vmt = new ValveMaterial(matPath, GameMount);
                        vtfPath = vmt.DiffusePath;
                    }
                    if (string.IsNullOrEmpty(vtfPath) || GameMount == null || !GameMount.HasFile(vtfPath, GameMount))
                    {
                        string directVtf = matPath.Substring(0, matPath.Length - 4) + ".vtf";
                        if (GameMount != null && GameMount.HasFile(directVtf, GameMount))
                            vtfPath = directVtf;
                    }
                }
                else
                {
                    vtfPath = matPath;
                }

                if (string.IsNullOrEmpty(vtfPath) || GameMount == null || !GameMount.HasFile(vtfPath, GameMount))
                {
                    Emit("error", $"Could not locate VTF texture file for '{texturePath}'.");
                    return false;
                }

                using (var stream = GameMount.OpenRead(vtfPath))
                using (var reader = new BinaryReader(stream))
                {
                    var vtf = new VTFFile(reader, true);
                    var img = vtf.HighResImage ?? vtf.LowResImage;
                    if (img == null)
                    {
                        Emit("error", $"VTF file '{vtfPath}' does not contain valid image data.");
                        return false;
                    }

                    Directory.CreateDirectory(outputRoot);
                    string safeName = Path.GetFileNameWithoutExtension(vtfPath);
                    string previewFile = Path.Combine(outputRoot, $"{safeName}_preview.png");
                    img.Save(previewFile, System.Drawing.Imaging.ImageFormat.Png);

                    var risk = AssessTextureRisk(texturePath);

                    Emit("texture_preview", $"Generated preview for {safeName}.", new
                    {
                        success = true,
                        texture = texturePath,
                        vtf = vtfPath,
                        path = previewFile,
                        width = img.Width,
                        height = img.Height,
                        format = vtf.Format.ToString(),
                        mipmaps = vtf.NumMipmaps,
                        riskLevel = risk.Level,
                        riskScore = risk.Score,
                        riskTags = risk.Tags,
                        riskReason = risk.Reason,
                        isRisky = risk.IsRisky
                    });
                    return true;
                }
            }
            catch (Exception ex)
            {
                Emit("error", $"Failed to generate texture preview for '{texturePath}': {ex.Message}", new { detail = ex.ToString() });
                return false;
            }
        }
    }
}
