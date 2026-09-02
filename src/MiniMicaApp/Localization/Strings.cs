using System.Globalization;
using System.Resources;
using System.Threading;

namespace MiniMicaApp.Localization
{
    /// <summary>
    /// Lightweight ResourceManager wrapper. No generated Strings.Designer.cs is
    /// required, keeping localization editor- and build-tool-independent.
    /// </summary>
    public static class Strings
    {
        // Derived from the type rather than hardcoded, so ANY renaming mechanism works:
        // dotnet new sourceName substitution, Visual Studio's Export Template, or a
        // manual rename. A literal "MiniMicaApp.Localization.Strings" only survives the
        // first of those, and the failure mode is silent - every lookup falls back to the
        // key name at runtime.
        private static readonly ResourceManager Manager =
            new ResourceManager(typeof(Strings).Namespace + ".Strings", typeof(Strings).Assembly);

        public static string Get(string key)
        {
            return Get(key, Thread.CurrentThread.CurrentUICulture);
        }

        public static string Get(string key, CultureInfo culture)
        {
            if (string.IsNullOrEmpty(key))
            {
                return string.Empty;
            }
            string value = Manager.GetString(key, culture);
            return value ?? key;
        }

        /// <summary>
        /// Expands the stable placeholders used by the MiniMica translation catalog.
        /// </summary>
        public static string Expand(string key, string productName, string version)
        {
            string value = Get(key);
            if (productName != null)
            {
                value = value.Replace("{ProductName}", productName);
            }
            if (version != null)
            {
                value = value.Replace("{M.m.build}", version);
            }
            return value;
        }
    }
}
