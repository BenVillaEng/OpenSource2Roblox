using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using Source2Roblox.Util;

namespace Source2Roblox.Views
{
    public partial class SettingsView : Page
    {
        private AppSettings settings;

        private static readonly (string Display, string Tag)[] SupportedLanguages =
        {
            ("System default",  ""),
            ("English",         "en"),
            ("Español",         "es"),
            ("Français",        "fr"),
            ("Deutsch",         "de"),
        };

        public SettingsView()
        {
            InitializeComponent();
            settings = SettingsManager.Load();

            foreach (var (display, _) in SupportedLanguages)
                LanguageComboBox.Items.Add(display);

            int langIdx = 0;
            for (int i = 0; i < SupportedLanguages.Length; i++)
            {
                if (SupportedLanguages[i].Tag == settings.LanguageOverride)
                {
                    langIdx = i;
                    break;
                }
            }
            LanguageComboBox.SelectedIndex = langIdx;

            ThemeComboBox.Items.Add("System");
            ThemeComboBox.Items.Add("Light");
            ThemeComboBox.Items.Add("Dark");
            ThemeComboBox.SelectedItem = settings.Theme;

            CustomTexturesTextBox.Text = settings.CustomTexturesDir;

            UploadAssetsToggle.IsChecked = settings.UploadAssets;
            UploadMeshesToggle.IsChecked = settings.UploadMeshes;
            ApiKeyTextBox.Text = settings.RobloxApiKey;
            
            CreatorTypeComboBox.Items.Add("User");
            CreatorTypeComboBox.Items.Add("Group");
            CreatorTypeComboBox.SelectedItem = string.IsNullOrEmpty(settings.RobloxCreatorType) ? "User" : settings.RobloxCreatorType;
            
            CreatorIdTextBox.Text = settings.RobloxCreatorId;
        }

        public void UpdateLanguage()
        {
            SettingsTitleLabel.Text         = LanguageManager.Get("Title_Settings");
            VisualOptionsLabel.Text         = "Visual & Language";
            LanguageLabel.Text              = LanguageManager.Get("Label_Language");
            LanguageDescLabel.Text          = LanguageManager.Get("Label_LanguageDesc");
            ThemeLabel.Text                 = LanguageManager.Get("Label_Theme");
            ThemeDescLabel.Text             = LanguageManager.Get("Label_ThemeDesc");
            CustomTexturesLabel.Text        = LanguageManager.Get("Label_CustomTextures");
            CustomTexturesDescLabel.Text    = LanguageManager.Get("Label_CustomTexturesDesc");
            AssetCachingLabel.Text          = LanguageManager.Get("Label_AssetCaching");
            AssetCachingDescLabel.Text      = LanguageManager.Get("Label_AssetCachingDesc");
            ClearCacheButton.Content        = LanguageManager.Get("Button_ClearCache");
            SaveSettingsButton.Content      = LanguageManager.Get("Button_SaveSettings");
            BrowseCustomTexturesButton.Content = LanguageManager.Get("Button_Browse");

            RobloxUploadLabel.Text          = LanguageManager.Get("Label_RobloxUpload");
            UploadAssetsLabel.Text          = LanguageManager.Get("Label_UploadAssets");
            UploadAssetsDescLabel.Text      = LanguageManager.Get("Label_UploadAssetsDesc");
            UploadMeshesLabel.Text          = LanguageManager.Get("Label_UploadMeshes");
            UploadMeshesDescLabel.Text      = LanguageManager.Get("Label_UploadMeshesDesc");
            ApiKeyLabel.Text                = LanguageManager.Get("Label_ApiKey");
            ApiKeyDescLabel.Text            = LanguageManager.Get("Label_ApiKeyDesc");
            CreatorTypeLabel.Text           = LanguageManager.Get("Label_CreatorType");
            CreatorTypeDescLabel.Text       = LanguageManager.Get("Label_CreatorTypeDesc");
            CreatorIdLabel.Text             = LanguageManager.Get("Label_CreatorId");
            CreatorIdDescLabel.Text         = LanguageManager.Get("Label_CreatorIdDesc");
        }

        private void BrowseCustomTexturesButton_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Select Custom Textures Folder";
                dialog.UseDescriptionForTitle = true;
                dialog.SelectedPath = settings.CustomTexturesDir;

                if (dialog.ShowDialog() == DialogResult.OK)
                    CustomTexturesTextBox.Text = dialog.SelectedPath;
            }
        }

        private void SaveSettingsButton_Click(object sender, RoutedEventArgs e)
        {
            settings.CustomTexturesDir = CustomTexturesTextBox.Text.Trim();
            settings.Theme             = ThemeComboBox.SelectedItem?.ToString() ?? "Dark";

            int idx = LanguageComboBox.SelectedIndex;
            settings.LanguageOverride = (idx >= 0 && idx < SupportedLanguages.Length)
                ? SupportedLanguages[idx].Tag
                : "";

            settings.UploadAssets = UploadAssetsToggle.IsChecked == true;
            settings.UploadMeshes = UploadMeshesToggle.IsChecked == true;
            settings.RobloxApiKey = ApiKeyTextBox.Text.Trim();
            settings.RobloxCreatorType = CreatorTypeComboBox.SelectedItem?.ToString() ?? "User";
            settings.RobloxCreatorId = CreatorIdTextBox.Text.Trim();

            SettingsManager.Save(settings);

            LanguageManager.SetCulture(settings.LanguageOverride);

            var mainWindow = Window.GetWindow(this) as MainWindow;
            mainWindow?.ApplyVisualSettings(settings);
            mainWindow?.UpdateLanguage();

            FeedbackText.Text = LanguageManager.Get("Feedback_Saved");
            FeedbackText.Foreground = System.Windows.Media.Brushes.Green;

            var timer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(3)
            };
            timer.Tick += (s, ev) => { FeedbackText.Text = ""; timer.Stop(); };
            timer.Start();
        }

        private void ClearCacheButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                AssetUploadCache.ClearCache();

                string cachePathNew = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "OpenSource2Roblox",
                    "upload_cache.json"
                );

                string cachePathOld = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "Source2Roblox",
                    "upload_cache.json"
                );

                bool deletedAny = false;

                if (File.Exists(cachePathNew))
                {
                    File.Delete(cachePathNew);
                    deletedAny = true;
                }

                if (File.Exists(cachePathOld))
                {
                    File.Delete(cachePathOld);
                    deletedAny = true;
                }

                if (deletedAny)
                {
                    FeedbackText.Text = LanguageManager.Get("Feedback_CacheCleared");
                }
                else
                {
                    FeedbackText.Text = LanguageManager.Get("Feedback_CacheEmpty");
                }
                FeedbackText.Foreground = System.Windows.Media.Brushes.Green;
            }
            catch (Exception ex)
            {
                FeedbackText.Text = $"Error: {ex.Message}";
                FeedbackText.Foreground = System.Windows.Media.Brushes.Red;
            }

            var timer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(3)
            };
            timer.Tick += (s, ev) => { FeedbackText.Text = ""; timer.Stop(); };
            timer.Start();
        }
    }
}
