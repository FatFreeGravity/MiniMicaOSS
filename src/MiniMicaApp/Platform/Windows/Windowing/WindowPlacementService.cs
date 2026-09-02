using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace MiniMicaApp.Platform.Windows.Windowing
{
    /// <summary>
    /// Per-monitor-DPI-aware placement helper for small utility windows.
    /// </summary>
    public static class WindowPlacementService
    {
        public static void Place(Window window, WorkAreaPlacement placement, Thickness margin)
        {
            if (window == null)
            {
                throw new ArgumentNullException("window");
            }

            IntPtr hwnd = new WindowInteropHelper(window).EnsureHandle();
            IntPtr monitor = NativeWindow.MonitorFromWindow(hwnd, NativeWindow.MonitorDefaultToNearest);
            if (monitor == IntPtr.Zero)
            {
                return;
            }

            NativeWindow.MonitorInfo info = new NativeWindow.MonitorInfo();
            info.Size = (uint)Marshal.SizeOf(typeof(NativeWindow.MonitorInfo));

            if (!NativeWindow.GetMonitorInfo(monitor, ref info))
            {
                return;
            }

            uint dpi = NativeWindow.GetDpiForWindow(hwnd);
            double scale = dpi > 0 ? dpi / 96.0 : 1.0;
            double width = ResolveWindowWidth(window);
            double height = ResolveWindowHeight(window);
            NativeWindow.Rect work = info.Work;

            double workLeft = work.Left / scale;
            double workTop = work.Top / scale;
            double workRight = work.Right / scale;
            double workBottom = work.Bottom / scale;

            double left;
            switch (placement)
            {
                case WorkAreaPlacement.TopLeft:
                case WorkAreaPlacement.BottomLeft:
                    left = workLeft + margin.Left;
                    break;
                case WorkAreaPlacement.TopRight:
                case WorkAreaPlacement.BottomRight:
                    left = workRight - width - margin.Right;
                    break;
                default:
                    left = workLeft + ((workRight - workLeft - width) / 2);
                    break;
            }

            double top;
            switch (placement)
            {
                case WorkAreaPlacement.TopLeft:
                case WorkAreaPlacement.TopRight:
                    top = workTop + margin.Top;
                    break;
                case WorkAreaPlacement.BottomLeft:
                case WorkAreaPlacement.BottomRight:
                    top = workBottom - height - margin.Bottom;
                    break;
                default:
                    top = workTop + ((workBottom - workTop - height) / 2);
                    break;
            }

            window.Left = left;
            window.Top = top;
        }

        public static uint GetDpi(Window window)
        {
            IntPtr hwnd = new WindowInteropHelper(window).EnsureHandle();
            uint dpi = NativeWindow.GetDpiForWindow(hwnd);
            return dpi == 0 ? 96u : dpi;
        }

        private static double ResolveWindowWidth(Window window)
        {
            return double.IsNaN(window.Width) ? Math.Max(window.ActualWidth, window.MinWidth) : window.Width;
        }

        private static double ResolveWindowHeight(Window window)
        {
            return double.IsNaN(window.Height) ? Math.Max(window.ActualHeight, window.MinHeight) : window.Height;
        }
    }

    public enum WorkAreaPlacement
    {
        Center,
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight
    }
}
