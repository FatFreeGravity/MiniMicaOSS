using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;
using MiniMicaApp.Configuration;
using MiniMicaApp.Shell;

namespace MiniMicaApp.Controls
{
    /// <summary>
    /// Hosts a fixed-size one-page application inside a resizable window. Larger
    /// viewports center the page; narrower viewports clip horizontally and expose
    /// RepeatButtons for deliberate left/right panning. The page is never scaled and
    /// MiniMica never vertically scrolls the whole page.
    /// </summary>
    [TemplatePart(Name = PartScroller, Type = typeof(ScrollViewer))]
    [TemplatePart(Name = PartPanLeft, Type = typeof(RepeatButton))]
    [TemplatePart(Name = PartPanRight, Type = typeof(RepeatButton))]
    public class FixedPageHost : ContentControl
    {
        private const string PartScroller = "PART_Scroller";
        private const string PartPanLeft = "PART_PanLeft";
        private const string PartPanRight = "PART_PanRight";
        private const double OffsetTolerance = 0.5;

        private ScrollViewer _scroller;
        private RepeatButton _panLeft;
        private RepeatButton _panRight;

        public FixedPageHost()
        {
            SetResourceReference(StyleProperty, "MiniMica.FixedPageHostStyle");
            Loaded += OnLoaded;
            SizeChanged += OnSizeChanged;
        }

        public static readonly DependencyProperty DesignWidthProperty = DependencyProperty.Register(
            "DesignWidth",
            typeof(double),
            typeof(FixedPageHost),
            new FrameworkPropertyMetadata(AppOptions.DesignWidth, OnLayoutContractChanged));

        public double DesignWidth
        {
            get { return (double)GetValue(DesignWidthProperty); }
            set { SetValue(DesignWidthProperty, value); }
        }

        public static readonly DependencyProperty DesignHeightProperty = DependencyProperty.Register(
            "DesignHeight",
            typeof(double),
            typeof(FixedPageHost),
            new FrameworkPropertyMetadata(AppOptions.DesignHeight, OnLayoutContractChanged));

        public double DesignHeight
        {
            get { return (double)GetValue(DesignHeightProperty); }
            set { SetValue(DesignHeightProperty, value); }
        }

        public static readonly DependencyProperty MinimumViewportWidthProperty = DependencyProperty.Register(
            "MinimumViewportWidth",
            typeof(double),
            typeof(FixedPageHost),
            new FrameworkPropertyMetadata(AppOptions.MinimumViewportWidth, OnLayoutContractChanged));

        public double MinimumViewportWidth
        {
            get { return (double)GetValue(MinimumViewportWidthProperty); }
            set { SetValue(MinimumViewportWidthProperty, value); }
        }

        public static readonly DependencyProperty PanStepProperty = DependencyProperty.Register(
            "PanStep",
            typeof(double),
            typeof(FixedPageHost),
            new FrameworkPropertyMetadata(24.0));

        public double PanStep
        {
            get { return (double)GetValue(PanStepProperty); }
            set { SetValue(PanStepProperty, value); }
        }

        public static readonly DependencyProperty EnforceWindowConstraintsProperty = DependencyProperty.Register(
            "EnforceWindowConstraints",
            typeof(bool),
            typeof(FixedPageHost),
            new FrameworkPropertyMetadata(true, OnLayoutContractChanged));

        public bool EnforceWindowConstraints
        {
            get { return (bool)GetValue(EnforceWindowConstraintsProperty); }
            set { SetValue(EnforceWindowConstraintsProperty, value); }
        }

        public override void OnApplyTemplate()
        {
            UnhookTemplateParts();
            base.OnApplyTemplate();

            _scroller = GetTemplateChild(PartScroller) as ScrollViewer;
            _panLeft = GetTemplateChild(PartPanLeft) as RepeatButton;
            _panRight = GetTemplateChild(PartPanRight) as RepeatButton;

            if (_scroller != null) _scroller.ScrollChanged += OnScrollChanged;
            if (_panLeft != null) _panLeft.Click += OnPanLeft;
            if (_panRight != null) _panRight.Click += OnPanRight;

            QueueViewportRefresh();
        }

        private static void OnLayoutContractChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            FixedPageHost host = d as FixedPageHost;
            if (host != null)
            {
                host.ApplyWindowConstraints();
                host.QueueViewportRefresh();
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            ApplyWindowConstraints();
            QueueViewportRefresh();
        }

        private void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            QueueViewportRefresh();
        }

        private void OnScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            UpdatePanButtons();
        }

        private void OnPanLeft(object sender, RoutedEventArgs e)
        {
            if (_scroller != null)
            {
                _scroller.ScrollToHorizontalOffset(Math.Max(0, _scroller.HorizontalOffset - PanStep));
            }
        }

        private void OnPanRight(object sender, RoutedEventArgs e)
        {
            if (_scroller != null)
            {
                _scroller.ScrollToHorizontalOffset(Math.Min(
                    _scroller.ScrollableWidth,
                    _scroller.HorizontalOffset + PanStep));
            }
        }

        private void ApplyWindowConstraints()
        {
            if (!EnforceWindowConstraints || !IsLoaded)
            {
                return;
            }

            Window window = Window.GetWindow(this);
            if (window == null)
            {
                return;
            }

            // No frame compensation needed: the contrast-theme outline is an overlay
            // (PART_ContrastFrame) and costs no client area, so the design canvas fits at
            // the declared window size in every theme.
            MiniMicaWindow miniMicaWindow = window as MiniMicaWindow;
            double titleBar = miniMicaWindow == null ? 0 : miniMicaWindow.TitleBarHeight;
            window.MinWidth = Math.Max(window.MinWidth, MinimumViewportWidth);
            window.MinHeight = Math.Max(window.MinHeight, DesignHeight + titleBar);
        }

        private void QueueViewportRefresh()
        {
            Dispatcher.BeginInvoke(new Action(UpdatePanButtons), DispatcherPriority.Background);
        }

        private void UpdatePanButtons()
        {
            if (_scroller == null || _panLeft == null || _panRight == null)
            {
                return;
            }

            bool overflow = _scroller.ScrollableWidth > OffsetTolerance;
            Visibility visibility = overflow ? Visibility.Visible : Visibility.Collapsed;
            _panLeft.Visibility = visibility;
            _panRight.Visibility = visibility;

            if (!overflow)
            {
                if (_scroller.HorizontalOffset > 0)
                {
                    _scroller.ScrollToHorizontalOffset(0);
                }
                return;
            }

            _panLeft.IsEnabled = _scroller.HorizontalOffset > OffsetTolerance;
            _panRight.IsEnabled = _scroller.HorizontalOffset < _scroller.ScrollableWidth - OffsetTolerance;
        }

        private void UnhookTemplateParts()
        {
            if (_scroller != null) _scroller.ScrollChanged -= OnScrollChanged;
            if (_panLeft != null) _panLeft.Click -= OnPanLeft;
            if (_panRight != null) _panRight.Click -= OnPanRight;
        }
    }
}
