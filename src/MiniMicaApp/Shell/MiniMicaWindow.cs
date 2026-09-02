using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shell;
using MiniMicaApp.Configuration;
using MiniMicaApp.Platform.Windows.Dwm;
using MiniMicaApp.Platform.Windows.Theme;
using MiniMicaApp.Settings;

namespace MiniMicaApp.Shell
{
    /// <summary>
    /// MiniMica v5 custom-chrome window. The application owns the visual title bar;
    /// Windows/WPF still own real window state, system commands, resizing and snap.
    /// </summary>
    [TemplatePart(Name = PartAppIcon, Type = typeof(Image))]
    [TemplatePart(Name = PartSettingsButton, Type = typeof(Button))]
    [TemplatePart(Name = PartMinimizeButton, Type = typeof(Button))]
    [TemplatePart(Name = PartMaximizeButton, Type = typeof(Button))]
    [TemplatePart(Name = PartCloseButton, Type = typeof(Button))]
    public class MiniMicaWindow : Window
    {
        private const string PartAppIcon = "PART_AppIcon";
        private const string PartSettingsButton = "PART_SettingsButton";
        private const string PartMinimizeButton = "PART_MinimizeButton";
        private const string PartMaximizeButton = "PART_MaximizeButton";
        private const string PartCloseButton = "PART_CloseButton";

        private readonly WindowAppearanceController _appearance;
        private readonly CustomChromeController _chromeController;
        private WindowChrome _windowChrome;
        private Image _appIcon;
        private Button _settingsButton;
        private Button _minimizeButton;
        private Button _maximizeButton;
        private Button _closeButton;

        public MiniMicaWindow()
        {
            SetResourceReference(StyleProperty, "MiniMica.WindowStyle");
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.CanResize;
            AllowsTransparency = false;
            // Not settable from the window Style: WindowStartupLocation is a CLR
            // property on Window, not a DependencyProperty.
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            Background = Brushes.Transparent;
            TitleBarHeight = AppOptions.TitleBarHeight;
            Backdrop = AppOptions.DefaultBackdrop;

            SettingsManager.Initialize();
            ThemeManager.Apply(Resources, SettingsManager.Current.Theme);

            _appearance = new WindowAppearanceController(this);
            _chromeController = new CustomChromeController(this);

            CommandBindings.Add(new CommandBinding(SystemCommands.CloseWindowCommand, OnCloseWindow));
            CommandBindings.Add(new CommandBinding(SystemCommands.MaximizeWindowCommand, OnMaximizeWindow, CanResizeWindow));
            CommandBindings.Add(new CommandBinding(SystemCommands.MinimizeWindowCommand, OnMinimizeWindow, CanMinimizeWindow));
            CommandBindings.Add(new CommandBinding(SystemCommands.RestoreWindowCommand, OnRestoreWindow, CanResizeWindow));
            CommandBindings.Add(new CommandBinding(SystemCommands.ShowSystemMenuCommand, OnShowSystemMenu));
            InputBindings.Add(new KeyBinding(SystemCommands.ShowSystemMenuCommand, new KeyGesture(Key.Space, ModifierKeys.Alt)));

            SettingsManager.SettingsChanged += OnSettingsChanged;
            Closed += OnClosed;

            ConfigureWindowChrome();
        }

        public static readonly DependencyProperty BackdropProperty = DependencyProperty.Register(
            "Backdrop",
            typeof(BackdropKind),
            typeof(MiniMicaWindow),
            new FrameworkPropertyMetadata(BackdropKind.Mica, OnAppearancePropertyChanged));

        public BackdropKind Backdrop
        {
            get { return (BackdropKind)GetValue(BackdropProperty); }
            set { SetValue(BackdropProperty, value); }
        }

        public static readonly DependencyProperty CornerPreferenceProperty = DependencyProperty.Register(
            "CornerPreference",
            typeof(WindowCornerPreference),
            typeof(MiniMicaWindow),
            new FrameworkPropertyMetadata(WindowCornerPreference.Round, OnAppearancePropertyChanged));

        public WindowCornerPreference CornerPreference
        {
            get { return (WindowCornerPreference)GetValue(CornerPreferenceProperty); }
            set { SetValue(CornerPreferenceProperty, value); }
        }

        public static readonly DependencyProperty TitleBarHeightProperty = DependencyProperty.Register(
            "TitleBarHeight",
            typeof(double),
            typeof(MiniMicaWindow),
            new FrameworkPropertyMetadata(AppOptions.TitleBarHeight, OnTitleBarHeightChanged));

        public double TitleBarHeight
        {
            get { return (double)GetValue(TitleBarHeightProperty); }
            set { SetValue(TitleBarHeightProperty, value); }
        }

        public static readonly DependencyProperty ShowSettingsButtonProperty = DependencyProperty.Register(
            "ShowSettingsButton",
            typeof(bool),
            typeof(MiniMicaWindow),
            new FrameworkPropertyMetadata(true));

        public bool ShowSettingsButton
        {
            get { return (bool)GetValue(ShowSettingsButtonProperty); }
            set { SetValue(ShowSettingsButtonProperty, value); }
        }

        public static readonly DependencyProperty ShowMinimizeButtonProperty = DependencyProperty.Register(
            "ShowMinimizeButton",
            typeof(bool),
            typeof(MiniMicaWindow),
            new FrameworkPropertyMetadata(true));

        public bool ShowMinimizeButton
        {
            get { return (bool)GetValue(ShowMinimizeButtonProperty); }
            set { SetValue(ShowMinimizeButtonProperty, value); }
        }

        public static readonly DependencyProperty ShowMaximizeButtonProperty = DependencyProperty.Register(
            "ShowMaximizeButton",
            typeof(bool),
            typeof(MiniMicaWindow),
            new FrameworkPropertyMetadata(true));

        public bool ShowMaximizeButton
        {
            get { return (bool)GetValue(ShowMaximizeButtonProperty); }
            set { SetValue(ShowMaximizeButtonProperty, value); }
        }

        /// <summary>
        /// True while Windows reports the pointer over the maximize button through the
        /// non-client messages that back Snap Layouts. Once WM_NCHITTEST answers
        /// HTMAXBUTTON the WPF button stops receiving mouse input, so its hover and
        /// pressed visuals are driven from here instead of IsMouseOver.
        /// </summary>
        private static readonly DependencyPropertyKey IsMaximizeButtonHoveredPropertyKey =
            DependencyProperty.RegisterReadOnly(
                "IsMaximizeButtonHovered",
                typeof(bool),
                typeof(MiniMicaWindow),
                new FrameworkPropertyMetadata(false));

        public static readonly DependencyProperty IsMaximizeButtonHoveredProperty =
            IsMaximizeButtonHoveredPropertyKey.DependencyProperty;

        public bool IsMaximizeButtonHovered
        {
            get { return (bool)GetValue(IsMaximizeButtonHoveredProperty); }
        }

        private static readonly DependencyPropertyKey IsMaximizeButtonPressedPropertyKey =
            DependencyProperty.RegisterReadOnly(
                "IsMaximizeButtonPressed",
                typeof(bool),
                typeof(MiniMicaWindow),
                new FrameworkPropertyMetadata(false));

        public static readonly DependencyProperty IsMaximizeButtonPressedProperty =
            IsMaximizeButtonPressedPropertyKey.DependencyProperty;

        public bool IsMaximizeButtonPressed
        {
            get { return (bool)GetValue(IsMaximizeButtonPressedProperty); }
        }

        /// <summary>
        /// Enables the Windows 11 Snap Layouts flyout over the custom maximize button.
        /// Set false to fall back to a plain WPF button.
        /// </summary>
        public static readonly DependencyProperty IsSnapLayoutEnabledProperty = DependencyProperty.Register(
            "IsSnapLayoutEnabled",
            typeof(bool),
            typeof(MiniMicaWindow),
            new FrameworkPropertyMetadata(true));

        public bool IsSnapLayoutEnabled
        {
            get { return (bool)GetValue(IsSnapLayoutEnabledProperty); }
            set { SetValue(IsSnapLayoutEnabledProperty, value); }
        }

        internal void SetMaximizeButtonHovered(bool value)
        {
            SetValue(IsMaximizeButtonHoveredPropertyKey, value);
        }

        internal void SetMaximizeButtonPressed(bool value)
        {
            SetValue(IsMaximizeButtonPressedPropertyKey, value);
        }

        public bool IsBackdropActive
        {
            get { return _appearance.IsBackdropActive; }
        }

        public event EventHandler AppearanceChanged;
        public event EventHandler SettingsRequested;

        public override void OnApplyTemplate()
        {
            UnhookTemplateParts();
            base.OnApplyTemplate();

            _appIcon = GetTemplateChild(PartAppIcon) as Image;
            _settingsButton = GetTemplateChild(PartSettingsButton) as Button;
            _minimizeButton = GetTemplateChild(PartMinimizeButton) as Button;
            _maximizeButton = GetTemplateChild(PartMaximizeButton) as Button;
            _closeButton = GetTemplateChild(PartCloseButton) as Button;

            if (_appIcon != null)
            {
                _appIcon.MouseLeftButtonDown += OnAppIconMouseLeftButtonDown;
                _appIcon.MouseRightButtonUp += OnAppIconMouseRightButtonUp;
            }
            if (_settingsButton != null) _settingsButton.Click += OnSettingsButtonClick;
            if (_minimizeButton != null) _minimizeButton.Click += OnMinimizeButtonClick;
            if (_maximizeButton != null) _maximizeButton.Click += OnMaximizeButtonClick;
            if (_closeButton != null) _closeButton.Click += OnCloseButtonClick;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            _appearance.Attach();
            _chromeController.Attach();
        }

        protected override void OnMouseRightButtonUp(MouseButtonEventArgs e)
        {
            base.OnMouseRightButtonUp(e);
            if (e.Handled || IsPointerOverInteractiveChrome(e.OriginalSource as DependencyObject))
            {
                return;
            }

            Point point = e.GetPosition(this);
            if (point.Y >= 0 && point.Y <= TitleBarHeight)
            {
                ShowSystemMenuAt(point);
                e.Handled = true;
            }
        }

        public void RefreshAppearance()
        {
            _appearance.Refresh();
        }

        internal void NotifyAppearanceChanged()
        {
            EventHandler handler = AppearanceChanged;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        internal void ShowSystemMenuAtScreenPixels(int screenX, int screenY)
        {
            Point logicalScreen = new Point(screenX, screenY);
            PresentationSource source = PresentationSource.FromVisual(this);
            if (source != null && source.CompositionTarget != null)
            {
                logicalScreen = source.CompositionTarget.TransformFromDevice.Transform(logicalScreen);
            }
            SystemCommands.ShowSystemMenu(this, logicalScreen);
        }

        private void ConfigureWindowChrome()
        {
            _windowChrome = new WindowChrome
            {
                CaptionHeight = TitleBarHeight,
                ResizeBorderThickness = new Thickness(8),
                GlassFrameThickness = new Thickness(1),
                CornerRadius = new CornerRadius(0)
            };
            WindowChrome.SetWindowChrome(this, _windowChrome);
        }

        private static void OnAppearancePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            MiniMicaWindow window = d as MiniMicaWindow;
            if (window != null)
            {
                window.RefreshAppearance();
            }
        }

        private static void OnTitleBarHeightChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            MiniMicaWindow window = d as MiniMicaWindow;
            if (window != null && window._windowChrome != null)
            {
                window._windowChrome.CaptionHeight = window.TitleBarHeight;
            }
        }

        private void OnSettingsChanged(object sender, EventArgs e)
        {
            RefreshAppearance();
        }

        /// <summary>
        /// Screen-pixel hit test against the maximize button, used by the WM_NCHITTEST
        /// handler that reports HTMAXBUTTON to enable Snap Layouts.
        /// </summary>
        internal bool IsPointOverMaximizeButton(int screenX, int screenY)
        {
            if (!IsSnapLayoutEnabled || _maximizeButton == null || !_maximizeButton.IsVisible)
            {
                return false;
            }

            try
            {
                Point local = _maximizeButton.PointFromScreen(new Point(screenX, screenY));
                return local.X >= 0
                    && local.Y >= 0
                    && local.X < _maximizeButton.ActualWidth
                    && local.Y < _maximizeButton.ActualHeight;
            }
            catch (InvalidOperationException)
            {
                // No PresentationSource yet (window closing or not yet shown).
                return false;
            }
        }

        internal void ToggleMaximizeRestore()
        {
            if (WindowState == WindowState.Maximized)
            {
                SystemCommands.RestoreWindow(this);
            }
            else
            {
                SystemCommands.MaximizeWindow(this);
            }
        }

        private void OnSettingsButtonClick(object sender, RoutedEventArgs e)
        {
            EventHandler handler = SettingsRequested;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
                return;
            }

            SettingsWindow dialog = new SettingsWindow();
            dialog.Owner = this;
            dialog.ShowDialog();
        }

        private void OnMinimizeButtonClick(object sender, RoutedEventArgs e)
        {
            SystemCommands.MinimizeWindow(this);
        }

        private void OnMaximizeButtonClick(object sender, RoutedEventArgs e)
        {
            ToggleMaximizeRestore();
        }

        private void OnCloseButtonClick(object sender, RoutedEventArgs e)
        {
            SystemCommands.CloseWindow(this);
        }

        private void OnAppIconMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount >= 2)
            {
                SystemCommands.CloseWindow(this);
            }
            else
            {
                ShowSystemMenuAt(new Point(0, TitleBarHeight));
            }
            e.Handled = true;
        }

        private void OnAppIconMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            ShowSystemMenuAt(e.GetPosition(this));
            e.Handled = true;
        }

        private void ShowSystemMenuAt(Point pointInWindow)
        {
            Point screenPoint = PointToScreen(pointInWindow);
            PresentationSource source = PresentationSource.FromVisual(this);
            if (source != null && source.CompositionTarget != null)
            {
                screenPoint = source.CompositionTarget.TransformFromDevice.Transform(screenPoint);
            }
            SystemCommands.ShowSystemMenu(this, screenPoint);
        }

        private void ShowSystemMenuDefault()
        {
            ShowSystemMenuAt(new Point(0, TitleBarHeight));
        }

        private bool IsPointerOverInteractiveChrome(DependencyObject source)
        {
            DependencyObject current = source;
            while (current != null && current != this)
            {
                if (current is Button || current == _appIcon)
                {
                    return true;
                }
                current = VisualTreeHelper.GetParent(current);
            }
            return false;
        }

        private void OnCloseWindow(object sender, ExecutedRoutedEventArgs e)
        {
            Close();
        }

        private void OnMaximizeWindow(object sender, ExecutedRoutedEventArgs e)
        {
            SystemCommands.MaximizeWindow(this);
        }

        private void OnMinimizeWindow(object sender, ExecutedRoutedEventArgs e)
        {
            SystemCommands.MinimizeWindow(this);
        }

        private void OnRestoreWindow(object sender, ExecutedRoutedEventArgs e)
        {
            SystemCommands.RestoreWindow(this);
        }

        private void OnShowSystemMenu(object sender, ExecutedRoutedEventArgs e)
        {
            ShowSystemMenuDefault();
        }

        private void CanResizeWindow(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = ResizeMode == ResizeMode.CanResize || ResizeMode == ResizeMode.CanResizeWithGrip;
        }

        private void CanMinimizeWindow(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = ResizeMode != ResizeMode.NoResize;
        }

        private void UnhookTemplateParts()
        {
            if (_appIcon != null)
            {
                _appIcon.MouseLeftButtonDown -= OnAppIconMouseLeftButtonDown;
                _appIcon.MouseRightButtonUp -= OnAppIconMouseRightButtonUp;
            }
            if (_settingsButton != null) _settingsButton.Click -= OnSettingsButtonClick;
            if (_minimizeButton != null) _minimizeButton.Click -= OnMinimizeButtonClick;
            if (_maximizeButton != null) _maximizeButton.Click -= OnMaximizeButtonClick;
            if (_closeButton != null) _closeButton.Click -= OnCloseButtonClick;
        }

        private void OnClosed(object sender, EventArgs e)
        {
            SettingsManager.SettingsChanged -= OnSettingsChanged;
            _appearance.Dispose();
            _chromeController.Dispose();
            UnhookTemplateParts();
        }
    }
}
