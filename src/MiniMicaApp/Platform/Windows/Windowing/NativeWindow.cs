using System;
using System.Runtime.InteropServices;

namespace MiniMicaApp.Platform.Windows.Windowing
{
    internal static class NativeWindow
    {
        internal const uint MonitorDefaultToNearest = 0x00000002;

        [DllImport("user32.dll")]
        internal static extern uint GetDpiForWindow(IntPtr hwnd);

        [DllImport("user32.dll")]
        internal static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint flags);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetMonitorInfo(IntPtr monitor, ref MonitorInfo info);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool GetWindowRect(IntPtr hwnd, out Rect rect);

        [StructLayout(LayoutKind.Sequential)]
        internal struct Point
        {
            public int X;
            public int Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct Rect
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        internal struct MonitorInfo
        {
            public uint Size;
            public Rect Monitor;
            public Rect Work;
            public uint Flags;
        }

        [StructLayout(LayoutKind.Sequential)]
        internal struct MinMaxInfo
        {
            public Point Reserved;
            public Point MaxSize;
            public Point MaxPosition;
            public Point MinTrackSize;
            public Point MaxTrackSize;
        }
    }
}
