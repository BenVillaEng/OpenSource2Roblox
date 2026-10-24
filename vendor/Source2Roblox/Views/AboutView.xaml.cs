using System;
using System.Windows;
using System.Windows.Controls;

namespace Source2Roblox.Views
{
    public partial class AboutView : Page
    {
        private const string AppVersion = "1.1.0";

        private static readonly PersonEntry[] Maintainers =
        {
            new PersonEntry("mustafaalios",       "Lead Developer",   "https://github.com/mustafaalios"),
        };

        private static readonly PersonEntry[] Contributors =
        {
            new PersonEntry("SenorLawyer",   "Original conversion base (non active contributer)",     "https://github.com/SenorLawyer"),
        };

        private string activeTab = "about";

        public AboutView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            VersionLabel.Text = $"v{AppVersion}";
            PopulatePeople(MaintainersPanel,      Maintainers);
            PopulatePeople(ContributorRowsPanel,  Contributors);
            UpdateLanguage();
        }

        public void UpdateLanguage()
        {
            AboutTitleLabel.Text = Util.LanguageManager.Get("Title_About");
            AboutDescLabel.Text = Util.LanguageManager.Get("About_PageDesc");
            TabAboutButton.Content = Util.LanguageManager.Get("About_TabAbout");
            TabContributorsButton.Content = Util.LanguageManager.Get("About_TabContributors");

            WhatTitleLabel.Text = Util.LanguageManager.Get("About_WhatTitle");
            WhatBody1Label.Text = Util.LanguageManager.Get("About_WhatBody1");
            WhatBody2Label.Text = Util.LanguageManager.Get("About_WhatBody2");

            FormatsTitleLabel.Text = Util.LanguageManager.Get("About_FormatsTitle");
            LinksTitleLabel.Text = Util.LanguageManager.Get("About_LinksTitle");
            RepoLinkButton.Content = Util.LanguageManager.Get("About_RepoLink");

            LicenseTitleLabel.Text = Util.LanguageManager.Get("About_LicenseTitle");
            LicenseBodyLabel.Text = Util.LanguageManager.Get("About_LicenseBody");

            MaintainersLabel.Text = Util.LanguageManager.Get("About_Maintainers");
            ContributorsLabel.Text = Util.LanguageManager.Get("About_Contributors");
        }

        private void TabButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Wpf.Ui.Controls.Button btn)
                SetActiveTab(btn.Tag as string ?? "about");
        }

        private void SetActiveTab(string tab)
        {
            activeTab = tab;

            TabAboutButton.Appearance        = tab == "about"        ? Wpf.Ui.Controls.ControlAppearance.Primary : Wpf.Ui.Controls.ControlAppearance.Secondary;
            TabContributorsButton.Appearance = tab == "contributors" ? Wpf.Ui.Controls.ControlAppearance.Primary : Wpf.Ui.Controls.ControlAppearance.Secondary;

            AboutPanel.Visibility        = tab == "about"        ? Visibility.Visible : Visibility.Collapsed;
            ContributorsPanel.Visibility = tab == "contributors" ? Visibility.Visible : Visibility.Collapsed;
        }

        private static void PopulatePeople(StackPanel panel, PersonEntry[] people)
        {
            panel.Children.Clear();
            foreach (var p in people)
            {
                var row = new Grid { Margin = new Thickness(0, 0, 0, 10) };
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = new System.Windows.GridLength(1, System.Windows.GridUnitType.Star) });
                row.ColumnDefinitions.Add(new ColumnDefinition { Width = System.Windows.GridLength.Auto });

                var left = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
                left.Children.Add(new TextBlock { Text = p.Name, FontSize = 13, FontWeight = FontWeights.SemiBold });
                var role = new TextBlock { Text = p.Role, FontSize = 12 };
                role.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorSecondaryBrush");
                left.Children.Add(role);
                Grid.SetColumn(left, 0);

                var link = new Wpf.Ui.Controls.HyperlinkButton
                {
                    Content = "GitHub",
                    NavigateUri = p.Url,
                    VerticalAlignment = VerticalAlignment.Center
                };
                Grid.SetColumn(link, 1);

                row.Children.Add(left);
                row.Children.Add(link);

                var sep = new Separator { Opacity = 0.2, Margin = new Thickness(0, 0, 0, 10) };
                var wrap = new StackPanel();
                wrap.Children.Add(row);
                wrap.Children.Add(sep);
                panel.Children.Add(wrap);
            }
        }

        private record PersonEntry(string Name, string Role, string Url);
    }
}
