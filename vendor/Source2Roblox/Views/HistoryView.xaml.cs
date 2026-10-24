using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Source2Roblox.Util;

namespace Source2Roblox.Views
{
    public partial class HistoryView : Page
    {
        private List<HistoryViewModel> allItems = new List<HistoryViewModel>();

        public HistoryView()
        {
            InitializeComponent();
            Loaded += (_, _) => Refresh();
        }

        public void Refresh()
        {
            var entries = ConversionHistoryManager.Load();
            allItems = entries.Select(e => new HistoryViewModel(e)).ToList();
            UpdateStats();
            ApplyFilter(SearchBox.Text);
        }

        private void UpdateStats()
        {
            if (allItems.Count == 0)
            {
                StatsBar.Visibility = Visibility.Collapsed;
                return;
            }

            StatsBar.Visibility = Visibility.Visible;

            int total = allItems.Count;
            int succeeded = allItems.Count(i => i.Entry.Succeeded);
            double rate = total > 0 ? (succeeded * 100.0 / total) : 0;

            StatsTotalLabel.Text = $"{total} conversion{(total != 1 ? "s" : "")}";
            StatsSuccessLabel.Text = $"{rate:F0}% success rate";

            var lastGame = allItems.FirstOrDefault()?.DisplayGame ?? string.Empty;
            if (!string.IsNullOrEmpty(lastGame))
                StatsLastGameLabel.Text = $"Last: {lastGame}";
            else
                StatsLastGameLabel.Text = string.Empty;
        }

        private void ApplyFilter(string query)
        {
            query = (query ?? string.Empty).Trim();

            IEnumerable<HistoryViewModel> filtered = allItems;
            if (!string.IsNullOrEmpty(query))
            {
                filtered = allItems.Where(i =>
                    i.DisplayTarget.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    i.DisplayGame.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    i.DisplayType.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0);
            }

            var list = filtered.ToList();
            HistoryList.ItemsSource = list;
            EmptyState.Visibility = list.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            HistoryList.Visibility = list.Count > 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilter(SearchBox.Text);
        }

        private void ClearHistoryButton_Click(object sender, RoutedEventArgs e)
        {
            var result = System.Windows.MessageBox.Show(
                "Are you sure you want to delete all conversion history? This cannot be undone.",
                "Clear History",
                System.Windows.MessageBoxButton.YesNo,
                System.Windows.MessageBoxImage.Warning);

            if (result == System.Windows.MessageBoxResult.Yes)
            {
                ConversionHistoryManager.Clear();
                Refresh();
            }
        }

        private void OpenFileButton_Click(object sender, RoutedEventArgs e)
        {
            string historyPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "OpenSource2Roblox",
                "history.json");

            if (!File.Exists(historyPath))
            {
                System.Windows.MessageBox.Show("No history file found yet. Run a conversion first.", "Info",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo(historyPath) { UseShellExecute = true });
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Could not open the file: {ex.Message}", "Error",
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        private void RerunButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Wpf.Ui.Controls.Button btn && btn.Tag is HistoryViewModel vm)
            {
                var homeView = (System.Windows.Application.Current.MainWindow as MainWindow)?.GetHomeView();
                if (homeView == null) return;

                homeView.LoadFromHistory(vm.Entry);

                if (System.Windows.Application.Current.MainWindow is MainWindow mw)
                    mw.NavigateTo("home");
            }
        }

        private void OpenOutputButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Wpf.Ui.Controls.Button btn && btn.Tag is HistoryViewModel vm)
            {
                string path = vm.Entry.OutputPath;
                if (!string.IsNullOrEmpty(path) && Directory.Exists(path))
                {
                    try
                    {
                        Process.Start(new ProcessStartInfo(path) { UseShellExecute = true });
                    }
                    catch { }
                }
            }
        }

        public void UpdateLanguage() { }
    }

    public class HistoryViewModel
    {
        public ConversionHistoryEntry Entry { get; }

        public string DisplayTarget    => Entry.DisplayTarget;
        public string DisplayType      => Entry.DisplayType;
        public string DisplayGame      => Entry.DisplayGame;
        public string DisplayTimestamp  => Entry.DisplayTimestamp;
        public string DisplayDuration  => Entry.DisplayDuration;
        public string StatusBadge      => Entry.Succeeded ? "✔" : "✘";
        public string ErrorMessage     => Entry.ErrorMessage;

        public string DisplayOutputPath => string.IsNullOrEmpty(Entry.OutputPath)
            ? string.Empty
            : $"→ {Entry.OutputPath}";

        public Visibility ErrorVisibility =>
            string.IsNullOrEmpty(Entry.ErrorMessage) ? Visibility.Collapsed : Visibility.Visible;

        public Visibility DurationVisibility =>
            string.IsNullOrEmpty(DisplayDuration) ? Visibility.Collapsed : Visibility.Visible;

        public Visibility OutputPathVisibility =>
            string.IsNullOrEmpty(Entry.OutputPath) ? Visibility.Collapsed : Visibility.Visible;

        public Visibility TexturesUploadedVisibility =>
            Entry.UploadedTextures ? Visibility.Visible : Visibility.Collapsed;

        public Visibility MeshesUploadedVisibility =>
            Entry.UploadedMeshes ? Visibility.Visible : Visibility.Collapsed;

        public Visibility BadgesVisibility =>
            (Entry.UploadedTextures || Entry.UploadedMeshes) ? Visibility.Visible : Visibility.Collapsed;

        public System.Windows.Media.Brush StatusBackground =>
            Entry.Succeeded
                ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(0x28, 0x2E, 0xCC, 0x71))
                : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(0x28, 0xE0, 0x50, 0x50));

        public HistoryViewModel(ConversionHistoryEntry entry)
        {
            Entry = entry;
        }
    }
}
