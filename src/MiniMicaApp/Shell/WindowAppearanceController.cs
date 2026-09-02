using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Interop;
using MiniMicaApp.Platform.Windows.Dwm;
using MiniMicaApp.Platform.Windows.Theme;
using MiniMicaApp.Settings;

namespace MiniMicaApp.Shell
{
    /// <summary>
    /// Centralizes live theme/backdrop refresh so derived windows do not implement
    /// their own WndProc or UpdateTheme methods.
    /// </summary>
    internal sealed class WindowAppearanceController : IDisposable
    {
        private const int WmSettingChange = 0x001A;
        private const int WmThemeChanged = 0x031A;
        private const int WmDwmCompositionChanged = 0x031E;

        private readonly MiniMicaWindow _window;
        private HwndSource _source;
        private bool _disposed;

        internal WindowAppearanceController(MiniMicaWindow window)
        {
            _window = window;
        }

        internal bool IsBackdropActive { get; private set; }

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
            SystemParameters.StaticPropertyChanged += OnSystemParametersChanged;
            Refresh();
        }

        internal void Refresh()
        {
            if (_disposed)
            {
                return;
            }

            ThemeManager.Apply(_window.Resources, SettingsManager.Current.Theme);
            bool dark = ThemeManager.IsDark(SettingsManager.Current.Theme);
            IntPtr hwnd = new WindowInteropHelper(_window).Handle;
            if (hwnd != IntPtr.Zero)
            {
                DwmWindow.ApplyDarkTitleBar(hwnd, dark);
                DwmWindow.ApplyCornerPreference(hwnd, _window.CornerPreference);
                BackdropKind backdrop = SystemParameters.HighContrast ? BackdropKind.None : _window.Backdrop;
                IsBackdropActive = DwmWindow.ApplyBackdrop(hwnd, backdrop);
            }
            else
            {
                IsBackdropActive = false;
            }

            _window.NotifyAppearanceChanged();
        }

        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == WmSettingChange || msg == WmThemeChanged || msg == WmDwmCompositionChanged)
            {
                _window.Dispatcher.BeginInvoke(new Action(Refresh));
            }
            return IntPtr.Zero;
        }

        private void OnSystemParametersChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "HighContrast")
            {
                Refresh();
            }
        }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }
            _disposed = true;
            SystemParameters.StaticPropertyChanged -= OnSystemParametersChanged;
            if (_source != null)
            {
                _source.RemoveHook(WndProc);
                _source = null;
            }
        }
    }
}
