using System;

namespace MiniMicaApp.Platform.Windows.Dwm
{
    public enum BackdropKind
    {
        Auto,
        Mica,
        Acrylic,
        Tabbed,
        None
    }

    public static class BackdropKindParser
    {
        public static BackdropKind Parse(string value, BackdropKind fallback = BackdropKind.Mica)
        {
            BackdropKind result;
            return Enum.TryParse(value, true, out result) ? result : fallback;
        }
    }
}
