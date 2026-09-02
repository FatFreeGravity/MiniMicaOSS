using System;
using MiniMicaApp.Configuration;
using MiniMicaApp.Platform.Windows.Theme;

namespace MiniMicaApp.Settings
{
    /// <summary>
    /// Process-wide view of the three global application preferences. The settings
    /// dialog changes these values immediately and open MiniMica windows refresh via
    /// SettingsChanged.
    /// </summary>
    public static class SettingsManager
    {
        private static AppSettings _current;

        public static event EventHandler SettingsChanged;

        public static AppSettings Current
        {
            get
            {
                EnsureInitialized();
                return _current;
            }
        }

        public static void Initialize()
        {
            if (_current == null)
            {
                _current = SettingsStore.Load();
            }
        }

        public static void SetTheme(AppTheme theme)
        {
            EnsureInitialized();
            if (_current.Theme == theme)
            {
                return;
            }
            _current.Theme = theme;
            SaveAndNotify();
        }

        public static void SetNotificationsEnabled(bool enabled)
        {
            EnsureInitialized();
            if (_current.NotificationsEnabled == enabled)
            {
                return;
            }
            _current.NotificationsEnabled = enabled;
            SaveAndNotify();
        }

        public static void SetTelemetryEnabled(bool enabled)
        {
            EnsureInitialized();
            if (_current.TelemetryEnabled == enabled)
            {
                return;
            }
            _current.TelemetryEnabled = enabled;
            SaveAndNotify();
        }

        /// <summary>
        /// Developer-only UI language override. "00" follows Windows. The value is
        /// persisted immediately but only takes effect on the next start, because XAML
        /// resolves localized strings as it is loaded - the Settings dialog offers a
        /// restart when this changes.
        /// </summary>
        public static void SetLanguage(string language)
        {
            EnsureInitialized();
            string value = string.IsNullOrWhiteSpace(language)
                ? AppOptions.DefaultLanguage
                : language.Trim();
            if (string.Equals(_current.Language, value, StringComparison.Ordinal))
            {
                return;
            }
            _current.Language = value;
            SaveAndNotify();
        }

        /// <summary>Full path of the per-user app.config, for installers and diagnostics.</summary>
        public static string ConfigPath
        {
            get { return SettingsStore.GetPath(); }
        }

        /// <summary>Deletes the per-user settings folder. Intended for uninstall.</summary>
        public static void Erase()
        {
            SettingsStore.Erase();
        }

        private static void EnsureInitialized()
        {
            if (_current == null)
            {
                Initialize();
            }
        }

        private static void SaveAndNotify()
        {
            try
            {
                SettingsStore.Save(_current);
            }
            catch
            {
                // Keep the in-process choice even if persistence fails. A derived app
                // may add diagnostics around this if its compliance policy requires it.
            }

            EventHandler handler = SettingsChanged;
            if (handler != null)
            {
                handler(null, EventArgs.Empty);
            }
        }
    }
}
