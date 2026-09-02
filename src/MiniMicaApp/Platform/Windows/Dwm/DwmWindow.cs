using System;

namespace MiniMicaApp.Platform.Windows.Dwm
{
    /// <summary>
    /// Thin wrapper around the documented DWM attributes MiniMica uses.
    ///
    /// Each call checks the build that introduced the attribute and returns quietly when
    /// it is unavailable, so on Windows 10 the window renders with its solid theme
    /// background and square corners instead of the app refusing to start.
    /// </summary>
    public static class DwmWindow
    {
        public static bool ApplyBackdrop(IntPtr hwnd, BackdropKind kind)
        {
            if (hwnd == IntPtr.Zero || !WindowsVersion.SupportsBackdrop)
            {
                return false;
            }

            int value;
            switch (kind)
            {
                case BackdropKind.Auto:
                    value = 0;
                    break;
                case BackdropKind.None:
                    value = 1;
                    break;
                case BackdropKind.Mica:
                    value = 2;
                    break;
                case BackdropKind.Acrylic:
                    value = 3;
                    break;
                case BackdropKind.Tabbed:
                    value = 4;
                    break;
                default:
                    value = 2;
                    break;
            }

            int hr = DwmNative.DwmSetWindowAttribute(
                hwnd,
                DwmNative.DwmwaSystemBackdropType,
                ref value,
                sizeof(int));

            if (hr < 0)
            {
                return false;
            }

            DwmNative.Margins margins;
            if (kind == BackdropKind.None)
            {
                // Undo a previous full-client frame extension when an application
                // turns Mica/Acrylic off or enters high contrast.
                margins = new DwmNative.Margins();
                DwmNative.DwmExtendFrameIntoClientArea(hwnd, ref margins);
                return false;
            }

            margins = new DwmNative.Margins
            {
                Left = -1,
                Right = -1,
                Top = -1,
                Bottom = -1
            };
            int frameHr = DwmNative.DwmExtendFrameIntoClientArea(hwnd, ref margins);
            return frameHr >= 0;
        }

        public static void ApplyDarkTitleBar(IntPtr hwnd, bool dark)
        {
            if (hwnd == IntPtr.Zero)
            {
                return;
            }

            int value = dark ? 1 : 0;
            DwmNative.DwmSetWindowAttribute(
                hwnd,
                DwmNative.DwmwaUseImmersiveDarkMode,
                ref value,
                sizeof(int));
        }

        public static void ApplyCornerPreference(IntPtr hwnd, WindowCornerPreference preference)
        {
            if (hwnd == IntPtr.Zero || !WindowsVersion.SupportsRoundedCorners)
            {
                return;
            }

            int value = (int)preference;
            DwmNative.DwmSetWindowAttribute(
                hwnd,
                DwmNative.DwmwaWindowCornerPreference,
                ref value,
                sizeof(int));
        }
    }

    public enum WindowCornerPreference
    {
        Default = 0,
        DoNotRound = 1,
        Round = 2,
        RoundSmall = 3
    }
}
