using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Wpf.Ui.Controls;
using Wpf.Ui.Appearance;
using Source2Roblox.Views;
using Source2Roblox.Util;

namespace Source2Roblox
{
    public partial class MainWindow : FluentWindow
    {
        private HomeView homeView;
        private SettingsView settingsView;
        private AboutView aboutView;
        private HistoryView historyView;
        private Dictionary<string, Page> pageMap;

        public MainWindow()
        {
            InitializeComponent();
            
            var settings = SettingsManager.Load();

            homeView = new HomeView();
            settingsView = new SettingsView();
            aboutView = new AboutView();
            historyView = new HistoryView();

            pageMap = new Dictionary<string, Page>
            {
                ["home"]     = homeView,
                ["settings"] = settingsView,
                ["history"]  = historyView,
                ["about"]    = aboutView,
            };

            LanguageManager.SetCulture(settings.LanguageOverride);

            RootNavigation.Loaded += RootNavigation_Loaded;

            RootFrame.Navigate(homeView);
            UpdateLanguage();

            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            var settings = SettingsManager.Load();
            ApplyVisualSettings(settings);
        }

        private void RootNavigation_Loaded(object sender, RoutedEventArgs e)
        {
            foreach (var obj in RootNavigation.MenuItems)
            {
                if (obj is NavigationViewItem item)
                    item.Click += NavItem_Click;
            }

            foreach (var obj in RootNavigation.FooterMenuItems)
            {
                if (obj is NavigationViewItem item)
                    item.Click += NavItem_Click;
            }
        }

        private void NavItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is NavigationViewItem item)
                NavigateTo(item.Tag as string ?? string.Empty);
        }

        public void ApplyVisualSettings(AppSettings settings)
        {
            WindowBackdropType bdt = WindowBackdropType.Mica;

            if (!WindowBackdrop.IsSupported(bdt))
            {
                bdt = WindowBackdrop.IsSupported(WindowBackdropType.Acrylic) ? WindowBackdropType.Acrylic : WindowBackdropType.None;
            }

            this.WindowBackdropType = bdt;

            if (settings.Theme == "Light")
                ApplicationThemeManager.Apply(ApplicationTheme.Light, bdt);
            else if (settings.Theme == "Dark")
                ApplicationThemeManager.Apply(ApplicationTheme.Dark, bdt);
            else
                ApplicationThemeManager.ApplySystemTheme();

            if (settings.Theme == "System")
            {
                SystemThemeWatcher.Watch(this, bdt);
            }
            else
            {
                if (IsLoaded)
                {
                    try { SystemThemeWatcher.UnWatch(this); } catch { }
                }
            }

            if (bdt == WindowBackdropType.None)
            {
                SetResourceReference(BackgroundProperty, "ApplicationBackgroundBrush");
            }
            else
            {
                Background = System.Windows.Media.Brushes.Transparent;
                WindowBackdrop.ApplyBackdrop(this, bdt);
            }

            var appTheme = settings.Theme;
            if (appTheme == "System")
            {
                appTheme = ApplicationThemeManager.GetAppTheme() == ApplicationTheme.Dark ? "Dark" : "Light";
            }

            var radialBrush = new System.Windows.Media.RadialGradientBrush
            {
                Center = new System.Windows.Point(0.5, 0.0),
                RadiusX = 1.2,
                RadiusY = 1.2,
                GradientOrigin = new System.Windows.Point(0.5, 0.0)
            };

            if (appTheme == "Dark")
            {
                radialBrush.GradientStops.Add(new System.Windows.Media.GradientStop(System.Windows.Media.Color.FromArgb(0xCC, 0x1E, 0x1E, 0x1E), 0.0));
                radialBrush.GradientStops.Add(new System.Windows.Media.GradientStop(System.Windows.Media.Color.FromArgb(0xF2, 0x0F, 0x0F, 0x0F), 1.0));
            }
            else
            {
                radialBrush.GradientStops.Add(new System.Windows.Media.GradientStop(System.Windows.Media.Color.FromArgb(0xEE, 0xFF, 0xFF, 0xFF), 0.0));
                radialBrush.GradientStops.Add(new System.Windows.Media.GradientStop(System.Windows.Media.Color.FromArgb(0xFA, 0xF5, 0xF5, 0xF5), 1.0));
            }

            if (RootGrid != null)
            {
                RootGrid.Background = radialBrush;
            }
        }

        public void UpdateLanguage()
        {
            var settings = SettingsManager.Load();
            LanguageManager.SetCulture(settings.LanguageOverride);

            Title = "OpenSource2Roblox Studio";

            foreach (var item in RootNavigation.MenuItems)
            {
                if (item is NavigationViewItem navItem)
                {
                    string tag = navItem.Tag as string ?? string.Empty;
                    if (tag == "home")     navItem.Content = LanguageManager.Get("Title_Home");
                    else if (tag == "settings") navItem.Content = LanguageManager.Get("Title_Settings");
                    else if (tag == "history")  navItem.Content = "History";
                }
            }

            foreach (var item in RootNavigation.FooterMenuItems)
            {
                if (item is NavigationViewItem navItem)
                {
                    string tag = navItem.Tag as string ?? string.Empty;
                    if (tag == "about")    navItem.Content = LanguageManager.Get("Title_About");
                }
            }

            homeView?.UpdateLanguage();
            settingsView?.UpdateLanguage();
            aboutView?.UpdateLanguage();
            historyView?.UpdateLanguage();
        }

        public HomeView GetHomeView() => homeView;

        public void NavigateTo(string tag)
        {
            if (tag == "history")
                historyView.Refresh();

            if (pageMap.TryGetValue(tag, out var page))
                RootFrame.Navigate(page);
        }
    }
}
