using System;
using System.Linq;
using System.Reflection;

namespace MiniMicaApp.Settings
{
    public static class AppVersion
    {
        public static string Display
        {
            get
            {
                Assembly assembly = Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
                AssemblyInformationalVersionAttribute attribute = assembly
                    .GetCustomAttributes(typeof(AssemblyInformationalVersionAttribute), false)
                    .OfType<AssemblyInformationalVersionAttribute>()
                    .FirstOrDefault();

                if (attribute != null && !string.IsNullOrWhiteSpace(attribute.InformationalVersion))
                {
                    return attribute.InformationalVersion;
                }

                // Major.Minor.Build, which is what the localized "settings_version"
                // string's {M.m.build} placeholder names and what v4.1 displayed.
                // Returning Major.Minor here dropped the build number entirely.
                Version version = assembly.GetName().Version;
                if (version == null)
                {
                    return "1.0.0";
                }
                return version.Major + "." + version.Minor + "." + version.Build;
            }
        }
    }
}
