using MiniMicaApp.Platform.Windows.Theme;

namespace MiniMicaApp.Settings
{
    /// <summary>
    /// The three OEM-facing preferences provided by MiniMica. Notification and
    /// telemetry values are policy switches only; the template does not implement
    /// notifications or telemetry itself.
    /// </summary>
    public sealed class AppSettings
    {
        public AppTheme Theme { get; set; }
        public bool NotificationsEnabled { get; set; }
        public bool TelemetryEnabled { get; set; }

        /// <summary>
        /// Developer-only UI language override, persisted as the "language" key.
        /// "00" (the default) means follow Windows; any other value is a culture name
        /// applied at startup. Exposed through the hidden Language row in the Settings
        /// dialog, revealed by opening it with Ctrl+Shift held.
        /// </summary>
        public string Language { get; set; }

        public AppSettings Clone()
        {
            return new AppSettings
            {
                Theme = Theme,
                NotificationsEnabled = NotificationsEnabled,
                TelemetryEnabled = TelemetryEnabled,
                Language = Language
            };
        }
    }
}
