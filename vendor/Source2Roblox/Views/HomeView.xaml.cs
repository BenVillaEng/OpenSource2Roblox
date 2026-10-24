using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media.Imaging;
using Source2Roblox.Util;

namespace Source2Roblox.Views
{
    public partial class HomeView : Page
    {
        private readonly ModeItem[] ModeItems = new[]
        {
            new ModeItem("Map",     "map"),
            new ModeItem("Model",   "model"),
            new ModeItem("Texture", "texture"),
            new ModeItem("Advanced","advanced"),
        };

        private string activeMode = "map";
        private List<GameInfo> discoveredGames = new List<GameInfo>();
        private GameInfo? selectedGame;
        private string previewPlacePath = string.Empty;
        private int logLineCount;
        private const int MaxLogLines = 2000;

        public HomeView()
        {
            InitializeComponent();
            
            ModeButtonsControl.ItemsSource = ModeItems;

            Program.OnEmit += Program_OnEmit;

            SetActiveMode(activeMode);

            RefreshGames();
        }

        private void RefreshGames()
        {
            discoveredGames = SteamScanner.ScanSourceGames();
            GamesComboBox.Items.Clear();
            foreach (var game in discoveredGames)
            {
                GamesComboBox.Items.Add(game.Name);
            }
        }

        private void SetActiveMode(string mode)
        {
            activeMode = mode;

            foreach (var item in ModeItems)
                item.IsActive = (item.Tag == mode);

            MapCard.Visibility = (mode == "map" || mode == "advanced") ? Visibility.Visible : Visibility.Collapsed;
            ModelCard.Visibility = (mode == "model" || mode == "advanced") ? Visibility.Visible : Visibility.Collapsed;
            TextureCard.Visibility = (mode == "texture" || mode == "advanced") ? Visibility.Visible : Visibility.Collapsed;
        }

        private void ModeButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Wpf.Ui.Controls.Button btn)
            {
                SetActiveMode(btn.Tag as string ?? "map");
            }
        }

        private void GamesComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int idx = GamesComboBox.SelectedIndex;
            if (idx >= 0 && idx < discoveredGames.Count)
            {
                selectedGame = discoveredGames[idx];
                GameDirTextBox.Text = selectedGame.GameDir;
                
                MapsComboBox.Items.Clear();
                foreach (var map in selectedGame.Maps)
                {
                    MapsComboBox.Items.Add(map);
                }
            }
        }

        private void GameDirTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string path = GameDirTextBox.Text.Trim();
            if (Directory.Exists(path) && File.Exists(Path.Combine(path, "gameinfo.txt")))
            {
                MapsComboBox.Items.Clear();
                string mapsDir = Path.Combine(path, "maps");
                if (Directory.Exists(mapsDir))
                {
                    try
                    {
                        foreach (string file in Directory.GetFiles(mapsDir, "*.bsp"))
                        {
                            MapsComboBox.Items.Add(Path.GetFileNameWithoutExtension(file));
                        }
                    }
                    catch { }
                }
            }
        }

        private void BrowseGameButton_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Select Source Game Mount Directory";
                dialog.UseDescriptionForTitle = true;
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    GameDirTextBox.Text = dialog.SelectedPath;
                }
            }
        }

        private void BrowseTextureButton_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new OpenFileDialog())
            {
                dialog.Filter = "Valve Texture Files (*.vtf)|*.vtf";
                dialog.Title = "Select VTF Texture File";
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    TextureTextBox.Text = dialog.FileName;
                }
            }
        }

        private void Program_OnEmit(string type, string message, object data)
        {
            Dispatcher.Invoke(() =>
            {
                logLineCount++;
                if (logLineCount > MaxLogLines)
                {
                    string text = LogTextBox.Text;
                    int trimAt = text.IndexOf('\n', text.Length / 4);
                    if (trimAt > 0)
                    {
                        LogTextBox.Text = text.Substring(trimAt + 1);
                        logLineCount = LogTextBox.Text.Count(c => c == '\n') + 1;
                    }
                }

                LogTextBox.AppendText($"[{type.ToUpper()}] {message}\r\n");
                if (type == "error" && data != null)
                {
                    try
                    {
                        var prop = data.GetType().GetProperty("detail");
                        string detail = prop?.GetValue(data)?.ToString();
                        if (!string.IsNullOrEmpty(detail))
                        {
                            LogTextBox.AppendText($"[ERROR DETAIL] {detail}\r\n");
                        }
                    }
                    catch { }
                }
                LogTextBox.ScrollToEnd();

                if (type == "progress")
                {
                    ProgressStatusText.Text = message;
                    StatusLabel.Text = message;
                }
                else if (type == "output")
                {
                    if (data != null)
                    {
                        try
                        {
                            var typeOfData = data.GetType();
                            var pathProp = typeOfData.GetProperty("path");
                            if (pathProp != null)
                            {
                                string val = pathProp.GetValue(data)?.ToString() ?? string.Empty;
                                if (!string.IsNullOrEmpty(val))
                                {
                                    if (val.EndsWith("maps") || val.Contains("maps"))
                                    {
                                        string bspName = MapsComboBox.Text.Trim();
                                        string parent = Path.GetDirectoryName(val);
                                        string rbxlPath = Path.Combine(parent, $"{bspName}.rbxl");
                                        if (File.Exists(rbxlPath))
                                        {
                                            previewPlacePath = rbxlPath;
                                            PreviewCard.Visibility = Visibility.Visible;
                                        }
                                    }
                                }
                            }
                        }
                        catch { }
                    }
                }
            });
        }

        private void ClearLogButton_Click(object sender, RoutedEventArgs e)
        {
            LogTextBox.Clear();
            logLineCount = 0;
        }

        private async void RunButton_Click(object sender, RoutedEventArgs e)
        {
            string gameDir = GameDirTextBox.Text.Trim();
            if (string.IsNullOrEmpty(gameDir) || !Directory.Exists(gameDir))
            {
                System.Windows.MessageBox.Show("Please select a valid game directory first.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var settings = SettingsManager.Load();
            settings.GameDir = gameDir;
            SettingsManager.Save(settings);

            var args = new List<string>();
            args.Add("-game");
            args.Add(gameDir);

            if (!string.IsNullOrEmpty(settings.CustomTexturesDir))
            {
                args.Add("-customTexturesDir");
                args.Add(settings.CustomTexturesDir);
            }

            if (settings.UploadAssets)
            {
                args.Add("-upload");
                args.Add("true");
            }

            if (settings.UploadMeshes)
            {
                args.Add("-uploadMeshes");
                args.Add("true");
            }

            if (!string.IsNullOrEmpty(settings.RobloxApiKey))
            {
                args.Add("-robloxApiKey");
                args.Add(settings.RobloxApiKey);
            }

            if (!string.IsNullOrEmpty(settings.RobloxCreatorType))
            {
                args.Add("-robloxCreatorType");
                args.Add(settings.RobloxCreatorType.ToLowerInvariant());
            }

            if (!string.IsNullOrEmpty(settings.RobloxCreatorId))
            {
                args.Add("-robloxCreatorId");
                args.Add(settings.RobloxCreatorId);
            }

            var historyEntry = new ConversionHistoryEntry
            {
                Timestamp       = DateTime.UtcNow,
                ConversionType  = activeMode,
                GameDir         = gameDir,
                GameName        = selectedGame?.Name ?? string.Empty,
                UploadedTextures = settings.UploadAssets,
                UploadedMeshes  = settings.UploadMeshes,
            };

            if (activeMode == "map")
            {
                string map = MapsComboBox.Text.Trim();
                if (string.IsNullOrEmpty(map))
                {
                    System.Windows.MessageBox.Show("Please enter or select a map name.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                args.Add("-map");
                args.Add(map);
                historyEntry.Target = map;
            }
            else if (activeMode == "model")
            {
                string model = ModelTextBox.Text.Trim();
                if (string.IsNullOrEmpty(model))
                {
                    System.Windows.MessageBox.Show("Please enter a model name/path.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                args.Add("-model");
                args.Add(model);
                historyEntry.Target = model;
            }
            else if (activeMode == "texture")
            {
                string tex = TextureTextBox.Text.Trim();
                if (string.IsNullOrEmpty(tex))
                {
                    System.Windows.MessageBox.Show("Please enter or browse a VTF texture path.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                args.Add("-vtf");
                args.Add(tex);
                historyEntry.Target = tex;
            }
            else if (activeMode == "advanced")
            {
                string map = MapsComboBox.Text.Trim();
                string model = ModelTextBox.Text.Trim();
                string tex = TextureTextBox.Text.Trim();

                if (!string.IsNullOrEmpty(map)) { args.Add("-map"); args.Add(map); }
                if (!string.IsNullOrEmpty(model)) { args.Add("-model"); args.Add(model); }
                if (!string.IsNullOrEmpty(tex)) { args.Add("-vtf"); args.Add(tex); }

                historyEntry.Target = string.Join(", ",
                    new[] { map, model, tex }.Where(s => !string.IsNullOrEmpty(s)));
            }

            RunButton.IsEnabled = false;
            ProgressPanel.Visibility = Visibility.Visible;
            StatusLabel.Text = "Converting...";
            LogTextBox.Clear();
            logLineCount = 0;
            PreviewCard.Visibility = Visibility.Collapsed;
            LauncherErrorText.Visibility = Visibility.Collapsed;

            string runError = null;
            var stopwatch = Stopwatch.StartNew();

            await Task.Run(() =>
            {
                try
                {
                    Program.Main(args.ToArray());
                }
                catch (Exception ex)
                {
                    runError = ex.Message;
                    Program.Emit("error", $"Fatal error: {ex.Message}", ex.ToString());
                }
            });

            stopwatch.Stop();

            historyEntry.Succeeded         = runError == null;
            historyEntry.ErrorMessage      = runError ?? string.Empty;
            historyEntry.DurationSeconds   = stopwatch.Elapsed.TotalSeconds;
            historyEntry.TextureCount      = 0; // Total texture count is not tracked at this level
            historyEntry.ExcludedTextureCount = Program.ExcludedTextures.Count;

            string outputRoot = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "Roblox Studio", "Source2Roblox Exports");
            historyEntry.OutputPath = outputRoot;

            ConversionHistoryManager.Append(historyEntry);

            RunButton.IsEnabled = true;
            ProgressPanel.Visibility = Visibility.Collapsed;
            StatusLabel.Text = runError == null ? "Done ✓" : "Failed ✘";
        }

        public void LoadFromHistory(ConversionHistoryEntry entry)
        {
            GameDirTextBox.Text = entry.GameDir;
            SetActiveMode(entry.ConversionType);

            switch (entry.ConversionType)
            {
                case "map":
                    MapsComboBox.Text = entry.Target;
                    break;
                case "model":
                    ModelTextBox.Text = entry.Target;
                    break;
                case "texture":
                    TextureTextBox.Text = entry.Target;
                    break;
            }
        }

        private void OpenPreviewButton_Click(object sender, RoutedEventArgs e)
        {
            LauncherErrorText.Visibility = Visibility.Collapsed;
            if (File.Exists(previewPlacePath))
            {
                try
                {
                    StudioLauncher.OpenInStudio(previewPlacePath);
                }
                catch (Exception ex)
                {
                    LauncherErrorText.Text = $"Failed to open Roblox Studio: {ex.Message}";
                    LauncherErrorText.Visibility = Visibility.Visible;
                }
            }
        }

        public void UpdateLanguage()
        {
            ConversionModeLabel.Text     = LanguageManager.Get("Label_Mode");
            ModeItems[0].Name = LanguageManager.Get("Mode_Map")     == "Mode_Map"     ? "Map"      : LanguageManager.Get("Mode_Map");
            ModeItems[1].Name = LanguageManager.Get("Mode_Model")   == "Mode_Model"   ? "Model"    : LanguageManager.Get("Mode_Model");
            ModeItems[2].Name = LanguageManager.Get("Mode_Texture") == "Mode_Texture" ? "Texture"  : LanguageManager.Get("Mode_Texture");
            ModeItems[3].Name = "Advanced";

            GameMountLabel.Text          = LanguageManager.Get("Label_Mount");
            SteamSelectLabel.Text        = LanguageManager.Get("Label_SteamSelect");
            ManualSelectLabel.Text       = LanguageManager.Get("Label_ManualSelect");
            BrowseGameButton.Content     = LanguageManager.Get("Button_Browse");

            MapDetailsLabel.Text         = LanguageManager.Get("Label_MapDetails");
            MapDescLabel.Text            = LanguageManager.Get("Label_MapDesc");

            ModelDetailsLabel.Text       = LanguageManager.Get("Label_ModelDetails");
            ModelDescLabel.Text          = LanguageManager.Get("Label_ModelDesc");

            TextureDetailsLabel.Text     = LanguageManager.Get("Label_TextureDetails");
            TextureDescLabel.Text        = LanguageManager.Get("Label_TextureDesc");
            BrowseTextureButton.Content  = LanguageManager.Get("Button_Browse");

            RunButton.Content            = LanguageManager.Get("Button_Convert");

            PreviewTitleLabel.Text       = LanguageManager.Get("Label_PreviewTitle");
            PreviewDescLabel.Text        = LanguageManager.Get("Label_PreviewDesc");
            OpenPreviewButton.Content    = LanguageManager.Get("Button_OpenPreview");

            OutputTitleLabel.Text        = LanguageManager.Get("Label_OutputTitle");
        }

        private void TextureCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (sender is System.Windows.Controls.CheckBox cb && cb.DataContext is TextureItem item)
            {
                item.IsChecked = cb.IsChecked ?? false;

                if (item.IsChecked)
                {
                    Program.ExcludedTextures.Remove(item.TexturePath);
                }
                else
                {
                    Program.ExcludedTextures.Add(item.TexturePath);
                }
            }
        }

        private async void ScanTexturesButton_Click(object sender, RoutedEventArgs e)
        {
            string gameDir = GameDirTextBox.Text.Trim();
            string map = MapsComboBox.Text.Trim();

            if (string.IsNullOrEmpty(gameDir) || !Directory.Exists(gameDir))
            {
                System.Windows.MessageBox.Show("Please select a valid game directory first.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrEmpty(map))
            {
                System.Windows.MessageBox.Show("Please enter or select a map name.", "Warning", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            ScanTexturesButton.IsEnabled = false;
            ProgressPanel.Visibility = Visibility.Visible;
            ProgressStatusText.Text = "Scanning map textures...";

            List<TextureItem> textureItems = new List<TextureItem>();
            Source2Roblox.FileSystem.GameMount gameMount = null;

            await Task.Run(() =>
            {
                try
                {
                    gameMount = new Source2Roblox.FileSystem.GameMount(gameDir);
                    string bspPath = Path.Combine(gameDir, "maps", $"{map}.bsp");

                    if (File.Exists(bspPath))
                    {
                        var bsp = new Source2Roblox.World.BSPFile(bspPath, gameMount);
                        var uniqueTextures = bsp.TexDataStringData.Values
                            .Select(t => t.ToLowerInvariant().Replace('\\', '/'))
                            .Distinct()
                            .OrderBy(t => t)
                            .ToList();

                        foreach (var tex in uniqueTextures)
                        {
                            if (tex.StartsWith("tools/") || tex == "tools/toolsnodraw" || tex == "tools/toolsblack")
                                continue;

                            var item = new TextureItem
                            {
                                DisplayName = tex,
                                TexturePath = tex,
                                IsChecked = !Program.ExcludedTextures.Contains(tex)
                            };

                            try
                            {
                                string vtfPath = $"materials/{tex}.vtf";
                                using var stream = gameMount.OpenRead(vtfPath);
                                if (stream != null)
                                {
                                    using var reader = new BinaryReader(stream);
                                    var vtf = new Source2Roblox.Textures.VTFFile(reader, true);
                                    item.Dimensions = $"{vtf.Width}×{vtf.Height}";

                                    var previewImage = vtf.LowResImage ?? vtf.HighResImage;
                                    if (previewImage is Bitmap bmp)
                                    {
                                        using var ms = new MemoryStream();
                                        bmp.Save(ms, ImageFormat.Png);
                                        ms.Position = 0;

                                        var bi = new BitmapImage();
                                        bi.BeginInit();
                                        bi.CacheOption = BitmapCacheOption.OnLoad;
                                        bi.StreamSource = ms;
                                        bi.DecodePixelWidth = 36;
                                        bi.EndInit();
                                        bi.Freeze();
                                        item.PreviewSource = bi;
                                    }
                                }
                            }
                            catch { }

                            textureItems.Add(item);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Dispatcher.Invoke(() => {
                        System.Windows.MessageBox.Show($"Failed to scan textures: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    });
                }
            });

            ScanTexturesButton.IsEnabled = true;
            ProgressPanel.Visibility = Visibility.Collapsed;

            if (textureItems.Count > 0)
            {
                TexturesItemsControl.ItemsSource = textureItems;
                TextureExclusionsCard.Visibility = Visibility.Visible;
            }
            else
            {
                TextureExclusionsCard.Visibility = Visibility.Collapsed;
                System.Windows.MessageBox.Show("No uploadable textures found on this map, or map file could not be parsed.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }

    public class TextureItem
    {
        public string DisplayName { get; set; } = string.Empty;
        public string TexturePath { get; set; } = string.Empty;
        public bool IsChecked { get; set; } = true;
        public BitmapSource PreviewSource { get; set; }
        public string Dimensions { get; set; } = string.Empty;
        public Visibility DimensionsVisibility =>
            string.IsNullOrEmpty(Dimensions) ? Visibility.Collapsed : Visibility.Visible;
    }

    public class ModeItem : System.ComponentModel.INotifyPropertyChanged
    {
        private string _name;
        private bool _isActive;

        public string Tag { get; }
        public string Name { get => _name; set { _name = value; OnPropertyChanged(); } }
        public bool IsActive { get => _isActive; set { _isActive = value; OnPropertyChanged(); OnPropertyChanged(nameof(Appearance)); } }

        public Wpf.Ui.Controls.ControlAppearance Appearance =>
            IsActive ? Wpf.Ui.Controls.ControlAppearance.Primary : Wpf.Ui.Controls.ControlAppearance.Secondary;

        public event System.ComponentModel.PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string name = null) =>
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(name));

        public ModeItem(string name, string tag)
        {
            _name = name;
            Tag = tag;
        }
    }
}
