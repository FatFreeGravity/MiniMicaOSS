using System.Windows;
using MiniMicaApp.Configuration;
using MiniMicaApp.Localization;
using MiniMicaApp.Platform.Windows;
using MiniMicaApp.Platform.Windows.Theme;
using MiniMicaApp.Settings;
using MiniMicaApp.Views;

namespace MiniMicaApp
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            if (!WindowsVersion.IsSupported)
            {
                MessageBox.Show(
                    AppOptions.DisplayName + " requires Windows 10 version 1903 (build 18362) or later.\n\nDetected: " + WindowsVersion.Description,
                    AppOptions.DisplayName,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Shutdown(1);
                return;
            }

            // Order matters. Settings load first because they carry the developer
            // language override, and the UI culture must be set before WPF creates any
            // FrameworkElement - XAML resolves localized strings as it loads.
            SettingsManager.Initialize();
            LocalizationManager.ApplyStoredLanguage(SettingsManager.Current.Language);
            // Per-culture layout metrics depend on the culture just applied, and must be in
            // place before any window measures its text.
            LocalizationManager.ApplyMetrics(Resources);
            ThemeManager.Apply(Resources, SettingsManager.Current.Theme);

            MainWindow window = new MainWindow();
            MainWindow = window;
            window.Show();
        }
    }
}
