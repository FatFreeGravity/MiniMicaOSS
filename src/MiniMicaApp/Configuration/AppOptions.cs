using System;
using System.Reflection;
using MiniMicaApp.Platform.Windows.Dwm;
using MiniMicaApp.Platform.Windows.Theme;

namespace MiniMicaApp.Configuration
{
    /// <summary>
    /// Product identity and the deliberately small set of defaults most forks change.
    /// </summary>
    public static class AppOptions
    {
        /// <summary>
        /// Set this to force a friendly product name ("Contoso Photo Suite"). Leave it
        /// empty and DisplayName follows the assembly, which means a rename works under
        /// any mechanism: dotnet new, Visual Studio's Export Template, or renaming the
        /// project by hand.
        /// </summary>
        private const string DisplayNameOverride = "";

        /// <summary>
        /// Product name shown in the title bar, dialog titles and message boxes.
        /// </summary>
        public static string DisplayName
        {
            get
            {
                if (!string.IsNullOrEmpty(DisplayNameOverride))
                {
                    return DisplayNameOverride;
                }

                Assembly assembly = Assembly.GetEntryAssembly() ?? typeof(AppOptions).Assembly;

                object[] product = assembly.GetCustomAttributes(typeof(AssemblyProductAttribute), false);
                if (product.Length > 0)
                {
                    string value = ((AssemblyProductAttribute)product[0]).Product;
                    if (!string.IsNullOrWhiteSpace(value))
                    {
                        return value;
                    }
                }

                return assembly.GetName().Name;
            }
        }

        // MiniMica's default fixed-page contract. Derived apps can override these on
        // FixedPageHost without changing the shell implementation.
        public const double DesignWidth = 960.0;
        public const double DesignHeight = 640.0;
        public const double MinimumViewportWidth = 500.0;
        public const double TitleBarHeight = 30.0;

        // Replaced by `dotnet new minimica`. Fallbacks keep the checked-in source
        // directly runnable before template expansion.
        private const string DefaultThemeName = "MINIMICA_THEME";
        private const string DefaultBackdropName = "MINIMICA_BACKDROP";

        public const bool DefaultNotificationsEnabled = true;
        public const bool DefaultTelemetryEnabled = false;

        // Per-user settings live at
        //   %LOCALAPPDATA%\<ConfigVendorFolder>\<ConfigFamilyFolder>\<assembly name>\app.config
        // v4.1 established OEM\MiniMica\<AppName> and derived applications read that exact
        // path, so it is the default. A fork wanting its own vendor grouping changes these
        // two values; the file name and format stay the same.
        public const string ConfigVendorFolder = "OEM";
        public const string ConfigFamilyFolder = "MiniMica";
        public const string ConfigFileName = "app.config";

        /// <summary>
        /// Developer language override. "00" means follow Windows. Any other value is a
        /// culture name that forces the UI language at startup - see the hidden Language
        /// row in the Settings dialog (open it holding Ctrl+Shift).
        /// </summary>
        public const string DefaultLanguage = "00";

        public static AppTheme DefaultTheme
        {
            get { return AppThemeParser.Parse(DefaultThemeName, AppTheme.System); }
        }

        public static BackdropKind DefaultBackdrop
        {
            get { return BackdropKindParser.Parse(DefaultBackdropName, BackdropKind.Mica); }
        }
    }
}
