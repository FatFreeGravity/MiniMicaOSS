using System;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MiniMicaApp.Configuration;
using MiniMicaApp.Localization;
using MiniMicaApp.Platform.Windows.Theme;
using MiniMicaApp.Shell;

namespace MiniMicaApp.Settings
{
    public partial class SettingsWindow : MiniMicaWindow
    {
        private bool _initializing = true;

        public SettingsWindow()
        {
            InitializeComponent();
            Title = AppOptions.DisplayName;
            SettingsTitle.Text = Strings.Expand("settings_title", AppOptions.DisplayName, null);
            VersionText.Text = Strings.Expand("settings_version", null, AppVersion.Display);

            AppSettings settings = SettingsManager.Current;
            AutomaticRadio.IsChecked = settings.Theme == AppTheme.System;
            LightRadio.IsChecked = settings.Theme == AppTheme.Light;
            DarkRadio.IsChecked = settings.Theme == AppTheme.Dark;
            NotificationsCheckBox.IsChecked = settings.NotificationsEnabled;
            TelemetryCheckBox.IsChecked = settings.TelemetryEnabled;

            // Developer language override: only offered when the dialog is opened with
            // Ctrl+Shift held, exactly as in v4.1.
            if ((Keyboard.Modifiers & (ModifierKeys.Control | ModifierKeys.Shift))
                == (ModifierKeys.Control | ModifierKeys.Shift))
            {
                PopulateLanguages(settings.Language);
                LanguageRow.Visibility = Visibility.Visible;
            }

            _initializing = false;
        }

        private void PopulateLanguages(string current)
        {
            LanguageCombo.Items.Add(new ComboBoxItem
            {
                Content = "System default (00)",
                Tag = LocalizationManager.SystemDefault
            });

            foreach (string culture in LocalizationManager.TestCultures)
            {
                string label;
                try
                {
                    label = CultureInfo.GetCultureInfo(culture).DisplayName + " (" + culture + ")";
                }
                catch (CultureNotFoundException)
                {
                    label = culture;
                }

                LanguageCombo.Items.Add(new ComboBoxItem { Content = label, Tag = culture });
            }

            LanguageCombo.SelectedValue = string.IsNullOrWhiteSpace(current)
                ? LocalizationManager.SystemDefault
                : current;

            if (LanguageCombo.SelectedItem == null)
            {
                LanguageCombo.SelectedIndex = 0;
            }
        }

        private void OnAutomaticClick(object sender, RoutedEventArgs e)
        {
            SettingsManager.SetTheme(AppTheme.System);
        }

        private void OnLightClick(object sender, RoutedEventArgs e)
        {
            SettingsManager.SetTheme(AppTheme.Light);
        }

        private void OnDarkClick(object sender, RoutedEventArgs e)
        {
            SettingsManager.SetTheme(AppTheme.Dark);
        }

        private void OnNotificationsClick(object sender, RoutedEventArgs e)
        {
            SettingsManager.SetNotificationsEnabled(NotificationsCheckBox.IsChecked == true);
        }

        private void OnTelemetryClick(object sender, RoutedEventArgs e)
        {
            SettingsManager.SetTelemetryEnabled(TelemetryCheckBox.IsChecked == true);
        }

        private void OnLanguageChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_initializing)
            {
                return;
            }

            ComboBoxItem item = LanguageCombo.SelectedItem as ComboBoxItem;
            string culture = item == null
                ? LocalizationManager.SystemDefault
                : (string)item.Tag;

            if (string.Equals(culture, SettingsManager.Current.Language, StringComparison.Ordinal))
            {
                return;
            }

            SettingsManager.SetLanguage(culture);

            // XAML resolves localized strings as it loads, so the change is only visible
            // after a restart. Offering it here keeps the developer loop short.
            MessageBoxResult answer = MessageBox.Show(
                this,
                "Language set to " + culture + ".\n\nRestart now to apply it?",
                AppOptions.DisplayName,
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (answer == MessageBoxResult.Yes)
            {
                Restart();
            }
        }

        private static void Restart()
        {
            try
            {
                Assembly entry = Assembly.GetEntryAssembly();
                string exe = entry == null ? null : entry.Location;
                if (string.IsNullOrEmpty(exe))
                {
                    return;
                }
                Process.Start(exe);
            }
            catch (Exception)
            {
                // The setting is already saved, so the next manual start picks it up.
                // A developer convenience must never take the app down unrecoverably.
                return;
            }

            Application.Current.Shutdown();
        }
    }
}
