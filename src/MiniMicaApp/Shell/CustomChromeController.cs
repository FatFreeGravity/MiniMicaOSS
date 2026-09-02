using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using MiniMicaApp.Platform.Windows.Windowing;

namespace MiniMicaApp.Shell
{
    /// <summary>
    /// Native compatibility for WindowStyle=None.
    ///
    /// Two jobs:
    /// 1. Keep maximized geometry inside the monitor work area and preserve the WPF
    ///    minimum-size contract (WM_GETMINMAXINFO).
    /// 2. Enable Windows 11 Snap Layouts over the custom maximize button by answering
    ///    HTMAXBUTTON to WM_NCHITTEST.
    ///
    /// Snap Layouts is why this class handles non-client mouse messages at all. Windows
    /// only offers the Snap flyout when hit testing reports HTMAXBUTTON, and the moment it
    /// does, that rectangle becomes non-client: the WPF Button underneath stops receiving
    /// mouse input entirely. Hover, press and click must therefore be driven from
    /// WM_NCMOUSEMOVE / WM_NCMOUSELEAVE / WM_NCLBUTTONDOWN / WM_NCLBUTTONUP and reflected
    /// onto the window through IsMaximizeButtonHovered / IsMaximizeButtonPressed, which the
    /// control template binds to. Drop any one of those handlers and the button goes
    /// visually dead even though the flyout still appears.
    ///
    /// It deliberately does not recreate v4.x pseudo-maximize state.
    /// </summary>
    internal sealed class CustomChromeController : IDisposable
    {
        private const int WmGetMinMaxInfo = 0x0024;
        private const int WmNcHitTest = 0x0084;
        private const int WmNcMouseMove = 0x00A0;
        private const int WmNcLButtonDown = 0x00A1;
        private const int WmNcLButtonUp = 0x00A2;
        private const int WmNcRButtonUp = 0x00A5;
        private const int WmNcMouseLeave = 0x02A2;
        private const int WmMouseMove = 0x0200;

        private const int HtCaption = 2;
        private const int HtMaxButton = 9;

        /// <summary>Matches WindowChrome.ResizeBorderThickness set in MiniMicaWindow.</summary>
        private const double ResizeBorderThickness = 8.0;

        private readonly MiniMicaWindow _window;
        private HwndSource _source;
        private bool _disposed;
        private bool _maxButtonPressed;

        internal CustomChromeController(MiniMicaWindow window)
        {
            _window = window;
        }

        internal void Attach()
        {
            if (_disposed || _source != null)
            {
                return;
            }

            IntPtr hwnd = new WindowInteropHelper(_window).Handle;
            _source = HwndSource.FromHwnd(hwnd);
            if (_source != null)
            {
                _source.AddHook(WndProc);
            }
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch (msg)
            {
                case WmGetMinMaxInfo:
                    ApplyWorkArea(hwnd, lParam);
                    handled = true;
                    return IntPtr.Zero;

                case WmNcHitTest:
                    return OnNcHitTest(hwnd, lParam, ref handled);

                case WmNcMouseMove:
                    // Movement over the HTMAXBUTTON region arrives here instead of in WPF,
                    // so the hover visual has to be maintained by hand.
                    SetHover(wParam.ToInt32() == HtMaxButton);
                    return IntPtr.Zero;

                case WmNcMouseLeave:
                    ClearMaxButtonState();
                    return IntPtr.Zero;

                case WmMouseMove:
                    // Pointer is back in the client area; drop any stale non-client hover.
                    if (_window.IsMaximizeButtonHovered || _window.IsMaximizeButtonPressed)
                    {
                        ClearMaxButtonState();
                    }
                    return IntPtr.Zero;

                case WmNcLButtonDown:
                    if (wParam.ToInt32() == HtMaxButton)
                    {
                        // Swallow it: the default handler would start a system
                        // maximize/size loop and the button would never look pressed.
                        _maxButtonPressed = true;
                        SetPressed(true);
                        handled = true;
                    }
                    return IntPtr.Zero;

                case WmNcLButtonUp:
                    if (wParam.ToInt32() == HtMaxButton)
                    {
                        bool wasPressed = _maxButtonPressed;
                        _maxButtonPressed = false;
                        SetPressed(false);
                        handled = true;
                        if (wasPressed)
                        {
                            // Deferred: changing window state inside the non-client
                            // message leaves Windows mid-gesture.
                            _window.Dispatcher.BeginInvoke(new Action(_window.ToggleMaximizeRestore));
                        }
                    }
                    return IntPtr.Zero;

                // WindowChrome normally handles title-bar right-click. Keep this native
                // fallback so the standard menu remains available across Windows 11 builds.
                case WmNcRButtonUp:
                    if (wParam.ToInt32() == HtCaption)
                    {
                        int rx = unchecked((short)(long)lParam);
                        int ry = unchecked((short)((long)lParam >> 16));
                        _window.ShowSystemMenuAtScreenPixels(rx, ry);
                        handled = true;
                    }
                    return IntPtr.Zero;
            }

            return IntPtr.Zero;
        }

        private IntPtr OnNcHitTest(IntPtr hwnd, IntPtr lParam, ref bool handled)
        {
            if (!_window.IsSnapLayoutEnabled)
            {
                return IntPtr.Zero;
            }

            int x = unchecked((short)(long)lParam);
            int y = unchecked((short)((long)lParam >> 16));

            if (!_window.IsPointOverMaximizeButton(x, y))
            {
                SetHover(false);
                return IntPtr.Zero;          // let WindowChrome answer
            }

            // Keep the top resize strip resizable while restored - Windows needs HTTOP
            // there. Maximized there is nothing to resize, so claim the whole button.
            if (_window.WindowState != WindowState.Maximized && IsWithinTopResizeBorder(hwnd, y))
            {
                SetHover(false);
                return IntPtr.Zero;
            }

            SetHover(true);
            handled = true;
            return new IntPtr(HtMaxButton);
        }

        private static bool IsWithinTopResizeBorder(IntPtr hwnd, int screenY)
        {
            NativeWindow.Rect rect;
            if (!NativeWindow.GetWindowRect(hwnd, out rect))
            {
                return false;
            }

            uint dpi = NativeWindow.GetDpiForWindow(hwnd);
            double scale = dpi > 0 ? dpi / 96.0 : 1.0;
            int border = (int)Math.Round(ResizeBorderThickness * scale);
            return screenY - rect.Top < border;
        }

        private void ClearMaxButtonState()
        {
            _maxButtonPressed = false;
            SetHover(false);
            SetPressed(false);
        }

        private void SetHover(bool value)
        {
            if (_window.IsMaximizeButtonHovered != value)
            {
                _window.SetMaximizeButtonHovered(value);
            }
        }

        private void SetPressed(bool value)
        {
            if (_window.IsMaximizeButtonPressed != value)
            {
                _window.SetMaximizeButtonPressed(value);
            }
        }

        private void ApplyWorkArea(IntPtr hwnd, IntPtr lParam)
        {
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

            NativeWindow.MinMaxInfo minMax = (NativeWindow.MinMaxInfo)Marshal.PtrToStructure(
                lParam,
                typeof(NativeWindow.MinMaxInfo));

            minMax.MaxPosition.X = info.Work.Left - info.Monitor.Left;
            minMax.MaxPosition.Y = info.Work.Top - info.Monitor.Top;
            minMax.MaxSize.X = info.Work.Right - info.Work.Left;
            minMax.MaxSize.Y = info.Work.Bottom - info.Work.Top;

            uint dpi = NativeWindow.GetDpiForWindow(hwnd);
            double scale = dpi > 0 ? dpi / 96.0 : 1.0;
            int minimumWidth = (int)Math.Ceiling(_window.MinWidth * scale);
            int minimumHeight = (int)Math.Ceiling(_window.MinHeight * scale);
            if (minimumWidth > minMax.MinTrackSize.X) minMax.MinTrackSize.X = minimumWidth;
            if (minimumHeight > minMax.MinTrackSize.Y) minMax.MinTrackSize.Y = minimumHeight;

            Marshal.StructureToPtr(minMax, lParam, true);
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }
            _disposed = true;
            if (_source != null)
            {
                _source.RemoveHook(WndProc);
                _source = null;
            }
        }
    }
}
