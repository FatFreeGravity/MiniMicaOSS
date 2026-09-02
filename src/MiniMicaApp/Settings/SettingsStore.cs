using System;
using System.Configuration;
using System.IO;
using System.Reflection;
using MiniMicaApp.Configuration;
using MiniMicaApp.Platform.Windows.Theme;

// System.Configuration.Configuration cannot be named directly from inside
// MiniMicaApp.Settings: the sibling namespace MiniMicaApp.Configuration (which holds
// AppOptions, and is imported above) wins name resolution, so a bare "Configuration"
// binds to a namespace and the compiler reports CS0118. A generated app hits the same
// collision - the namespace becomes <AppName>.Configuration - so this alias is the
// rename-safe fix rather than fully qualifying each use.
using ConfigFile = System.Configuration.Configuration;

namespace MiniMicaApp.Settings
{
    /// <summary>
    /// Per-user settings, stored as a standard .NET <c>appSettings</c> file at
    /// <c>%LOCALAPPDATA%\OEM\MiniMica\&lt;AppName&gt;\app.config</c>.
    ///
    /// The location, file name and key/value encoding are inherited from MiniMica v4.1
    /// and form a compatibility contract: derived OEM applications and installers read
    /// this file directly. It is written through <see cref="ConfigurationManager"/> so
    /// those consumers can open it with the standard API instead of parsing XML by hand.
    ///
    /// <code>
    /// &lt;configuration&gt;
    ///   &lt;appSettings&gt;
    ///     &lt;add key="appearance"   value="2"  /&gt;  &lt;!-- 0=Dark 1=Light 2=Automatic --&gt;
    ///     &lt;add key="notification" value="1"  /&gt;  &lt;!-- 0=Off  1=On --&gt;
    ///     &lt;add key="diagnostics"  value="0"  /&gt;  &lt;!-- 0=Off  1=On --&gt;
    ///     &lt;add key="language"     value="00" /&gt;  &lt;!-- 00=follow Windows --&gt;
    ///   &lt;/appSettings&gt;
    /// &lt;/configuration&gt;
    /// </code>
    ///
    /// Do not change the keys or the numeric encoding without a migration. The numbers
    /// are deliberately NOT the <see cref="AppTheme"/> enum order: the enum is
    /// System=0, Light=1, Dark=2, while the file uses Dark=0, Light=1, Automatic=2.
    /// </summary>
    internal static class SettingsStore
    {
        internal const string KeyAppearance = "appearance";
        internal const string KeyNotification = "notification";
        internal const string KeyDiagnostics = "diagnostics";
        internal const string KeyLanguage = "language";

        internal static AppSettings Load()
        {
            AppSettings defaults = CreateDefaults();

            try
            {
                ConfigFile config = OpenConfiguration();
                KeyValueConfigurationCollection settings = config.AppSettings.Settings;
                if (settings.Count == 0)
                {
                    return defaults;
                }

                AppSettings loaded = defaults.Clone();
                loaded.Theme = ParseAppearance(Read(settings, KeyAppearance), defaults.Theme);
                loaded.NotificationsEnabled = ParseFlag(Read(settings, KeyNotification), defaults.NotificationsEnabled);
                loaded.TelemetryEnabled = ParseFlag(Read(settings, KeyDiagnostics), defaults.TelemetryEnabled);

                string language = Read(settings, KeyLanguage);
                loaded.Language = string.IsNullOrWhiteSpace(language)
                    ? AppOptions.DefaultLanguage
                    : language.Trim();

                return loaded;
            }
            catch
            {
                // A damaged or unreadable config must never stop a preloaded OEM utility
                // from starting. Defaults are the deterministic recovery path.
                return defaults;
            }
        }

        internal static void Save(AppSettings settings)
        {
            ConfigFile config = OpenConfiguration();
            KeyValueConfigurationCollection values = config.AppSettings.Settings;

            Write(values, KeyAppearance, FormatAppearance(settings.Theme));
            Write(values, KeyNotification, settings.NotificationsEnabled ? "1" : "0");
            Write(values, KeyDiagnostics, settings.TelemetryEnabled ? "1" : "0");
            Write(values, KeyLanguage, string.IsNullOrWhiteSpace(settings.Language)
                ? AppOptions.DefaultLanguage
                : settings.Language);

            config.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection(config.AppSettings.SectionInformation.Name);
        }

        /// <summary>
        /// Full path of the per-user configuration file, so an installer or uninstaller
        /// in a derived application can locate it.
        /// </summary>
        internal static string GetPath()
        {
            string appName = Assembly.GetEntryAssembly() == null
                ? AppOptions.DisplayName
                : Assembly.GetEntryAssembly().GetName().Name;

            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                AppOptions.ConfigVendorFolder,
                AppOptions.ConfigFamilyFolder,
                appName,
                AppOptions.ConfigFileName);
        }

        /// <summary>
        /// Removes this application's settings folder, then any now-empty family and
        /// vendor folders above it. Intended for uninstall; never called by the app.
        /// </summary>
        internal static void Erase()
        {
            try
            {
                string appFolder = Path.GetDirectoryName(GetPath());
                if (!string.IsNullOrEmpty(appFolder) && Directory.Exists(appFolder))
                {
                    Directory.Delete(appFolder, true);
                }

                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                DeleteIfEmpty(Path.Combine(localAppData, AppOptions.ConfigVendorFolder, AppOptions.ConfigFamilyFolder));
                DeleteIfEmpty(Path.Combine(localAppData, AppOptions.ConfigVendorFolder));
            }
            catch
            {
                // Uninstall cleanup is best-effort.
            }
        }

        private static void DeleteIfEmpty(string folder)
        {
            if (Directory.Exists(folder)
                && Directory.GetDirectories(folder).Length == 0
                && Directory.GetFiles(folder).Length == 0)
            {
                Directory.Delete(folder, true);
            }
        }

        private static ConfigFile OpenConfiguration()
        {
            string path = GetPath();
            string directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            ExeConfigurationFileMap map = new ExeConfigurationFileMap();
            map.ExeConfigFilename = path;
            return ConfigurationManager.OpenMappedExeConfiguration(map, ConfigurationUserLevel.None);
        }

        private static string Read(KeyValueConfigurationCollection settings, string key)
        {
            KeyValueConfigurationElement element = settings[key];
            return element == null ? string.Empty : element.Value;
        }

        private static void Write(KeyValueConfigurationCollection settings, string key, string value)
        {
            if (settings[key] == null)
            {
                settings.Add(key, value);
            }
            else
            {
                settings[key].Value = value;
            }
        }

        // File encoding is v4.1's: 0=Dark, 1=Light, 2=Automatic.
        private static string FormatAppearance(AppTheme theme)
        {
            if (theme == AppTheme.Dark) return "0";
            if (theme == AppTheme.Light) return "1";
            return "2";
        }

        private static AppTheme ParseAppearance(string value, AppTheme fallback)
        {
            if (value == "0") return AppTheme.Dark;
            if (value == "1") return AppTheme.Light;
            if (value == "2") return AppTheme.System;
            return fallback;
        }

        private static bool ParseFlag(string value, bool fallback)
        {
            if (value == "1") return true;
            if (value == "0") return false;
            return fallback;
        }

        private static AppSettings CreateDefaults()
        {
            return new AppSettings
            {
                Theme = AppOptions.DefaultTheme,
                NotificationsEnabled = AppOptions.DefaultNotificationsEnabled,
                TelemetryEnabled = AppOptions.DefaultTelemetryEnabled,
                Language = AppOptions.DefaultLanguage
            };
        }
    }
}
