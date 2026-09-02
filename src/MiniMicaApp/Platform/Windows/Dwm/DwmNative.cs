using System;
using System.Runtime.InteropServices;

namespace MiniMicaApp.Platform.Windows.Dwm
{
    internal static class DwmNative
    {
        internal const int DwmwaUseImmersiveDarkMode = 20;
        internal const int DwmwaWindowCornerPreference = 33;
        internal const int DwmwaSystemBackdropType = 38;

        [DllImport("dwmapi.dll", PreserveSig = true)]
        internal static extern int DwmSetWindowAttribute(
            IntPtr hwnd,
            int attribute,
            ref int attributeValue,
            int attributeSize);

        [DllImport("dwmapi.dll", PreserveSig = true)]
        internal static extern int DwmExtendFrameIntoClientArea(IntPtr hwnd, ref Margins margins);

        [StructLayout(LayoutKind.Sequential)]
        internal struct Margins
        {
            public int Left;
            public int Right;
            public int Top;
            public int Bottom;
        }
    }
}
