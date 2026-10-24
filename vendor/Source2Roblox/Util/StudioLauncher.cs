using System;
using System.Diagnostics;
using System.IO;

namespace Source2Roblox.Util
{
    public static class StudioLauncher
    {
        public static void OpenInStudio(string targetPath)
        {
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            
            string[] candidates = {
                Path.Combine(localAppData, "Microsoft", "WinGet", "Links", "RobloxStudioModManager.exe"),
                Path.Combine(localAppData, "Roblox Studio Mod Manager", "RobloxStudioModManager.exe")
            };

            string executablePath = null;
            foreach (var candidate in candidates)
            {
                if (File.Exists(candidate))
                {
                    executablePath = candidate;
                    break;
                }
            }

            if (string.IsNullOrEmpty(executablePath))
            {
                // Fallback to standard Roblox Studio
                string robloxVersionsDir = Path.Combine(localAppData, "Roblox", "Versions");
                if (Directory.Exists(robloxVersionsDir))
                {
                    try
                    {
                        foreach (var dir in Directory.GetDirectories(robloxVersionsDir))
                        {
                            string candidate = Path.Combine(dir, "RobloxStudioBeta.exe");
                            if (File.Exists(candidate))
                            {
                                executablePath = candidate;
                                break;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error scanning standard Roblox versions: {ex.Message}");
                    }
                }
            }

            if (string.IsNullOrEmpty(executablePath))
            {
                throw new FileNotFoundException("Neither Roblox Studio nor Roblox Studio Mod Manager could be found. Please ensure Roblox Studio is installed.");
            }

            Process.Start(new ProcessStartInfo
            {
                FileName = executablePath,
                Arguments = $"\"{targetPath}\"",
                UseShellExecute = true
            });
        }
    }
}
