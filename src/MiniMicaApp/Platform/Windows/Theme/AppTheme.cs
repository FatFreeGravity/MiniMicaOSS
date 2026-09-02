using System;

namespace MiniMicaApp.Platform.Windows.Theme
{
    public enum AppTheme
    {
        System,
        Light,
        Dark
    }

    public static class AppThemeParser
    {
        public static AppTheme Parse(string value, AppTheme fallback = AppTheme.System)
        {
            AppTheme result;
            return Enum.TryParse(value, true, out result) ? result : fallback;
        }
    }
}
