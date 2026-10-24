using System;
using System.IO;
using Newtonsoft.Json;

namespace Source2Roblox.Util
{
    public class AppSettings
    {
        public string CustomTexturesDir { get; set; } = string.Empty;
        public string GameDir { get; set; } = string.Empty;
        public string SelectedGameId { get; set; } = string.Empty;
        public string LanguageOverride { get; set; } = "";
        public string Theme { get; set; } = "System";
        public string Backdrop { get; set; } = "Mica";
        public bool UploadAssets { get; set; } = false;
        public bool UploadMeshes { get; set; } = false;
        public string RobloxApiKey { get; set; } = string.Empty;
        public string RobloxCreatorType { get; set; } = "User";
        public string RobloxCreatorId { get; set; } = string.Empty;
    }

    public static class SettingsManager
    {
        private static readonly string SettingsPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "OpenSource2Roblox",
            "settings.json"
        );

        public static AppSettings Load()
        {
            AppSettings settings = null;
            try
            {
                if (File.Exists(SettingsPath))
                {
                    string json = File.ReadAllText(SettingsPath);
                    settings = JsonConvert.DeserializeObject<AppSettings>(json);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading settings: {ex.Message}");
            }

            if (settings == null)
            {
                settings = new AppSettings();
            }

            if (string.IsNullOrEmpty(settings.RobloxApiKey))
            {
                try
                {
                    string oldPath = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                        "source2roblox-revamp",
                        "settings.json"
                    );

                    if (File.Exists(oldPath))
                    {
                        string oldJson = File.ReadAllText(oldPath);
                        var oldData = JsonConvert.DeserializeObject<Newtonsoft.Json.Linq.JObject>(oldJson);
                        var oldSettings = oldData?["settings"];
                        if (oldSettings != null)
                        {
                            settings.RobloxApiKey = oldSettings.Value<string>("robloxApiKey") ?? string.Empty;
                            settings.RobloxCreatorId = oldSettings.Value<string>("robloxCreatorId") ?? string.Empty;
                            settings.RobloxCreatorType = oldSettings.Value<string>("robloxCreatorType") ?? "User";
                            
                            if (!string.IsNullOrEmpty(settings.RobloxCreatorType))
                            {
                                settings.RobloxCreatorType = char.ToUpper(settings.RobloxCreatorType[0]) + settings.RobloxCreatorType.Substring(1).ToLowerInvariant();
                            }

                            settings.UploadAssets = oldSettings.Value<bool?>("uploadAssets") ?? false;
                            settings.UploadMeshes = oldSettings.Value<bool?>("uploadMeshes") ?? false;
                            settings.CustomTexturesDir = oldSettings.Value<string>("customTexturesDir") ?? string.Empty;

                            Save(settings);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error migrating settings: {ex.Message}");
                }
            }

            return settings;
        }

        public static void Save(AppSettings settings)
        {
            try
            {
                string dir = Path.GetDirectoryName(SettingsPath);
                if (!Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                string json = JsonConvert.SerializeObject(settings, Formatting.Indented);
                File.WriteAllText(SettingsPath, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error saving settings: {ex.Message}");
            }
        }
    }
}
