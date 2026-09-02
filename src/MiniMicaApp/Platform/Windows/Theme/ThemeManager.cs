using System;
using Microsoft.Win32;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace MiniMicaApp.Platform.Windows.Theme
{
    /// <summary>
    /// Dependency-free .NET Framework 4.8 theme helper. MiniMica owns a small set of
    /// semantic resources rather than relying on framework-specific Fluent packages.
    /// </summary>
    public static class ThemeManager
    {
        private const string PersonalizeKey = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
        private const string AppsUseLightThemeValue = "AppsUseLightTheme";

        /// <summary>
        /// Window outline thickness under a Windows contrast theme, in DIP. v4.1's
        /// MiniMicaGUI.WindowFrameThickness. Thin values disappear into the rounded
        /// corner, so this needs to be substantial to read as a frame.
        /// </summary>
        public const double HighContrastFrameThickness = 4.0;

        public static AppTheme Resolve(AppTheme preference)
        {
            if (SystemParameters.HighContrast)
            {
                return IsHighContrastDark() ? AppTheme.Dark : AppTheme.Light;
            }
            return preference == AppTheme.System ? ReadSystemTheme() : preference;
        }

        public static bool IsDark(AppTheme preference)
        {
            return Resolve(preference) == AppTheme.Dark;
        }

        public static void Apply(ResourceDictionary resources, AppTheme preference)
        {
            ApplySemanticPalette(resources, Resolve(preference), SystemParameters.HighContrast);
        }

        private static AppTheme ReadSystemTheme()
        {
            try
            {
                using (RegistryKey key = Registry.CurrentUser.OpenSubKey(PersonalizeKey, false))
                {
                    object value = key == null ? null : key.GetValue(AppsUseLightThemeValue);
                    return value is int && (int)value == 0 ? AppTheme.Dark : AppTheme.Light;
                }
            }
            catch
            {
                return AppTheme.Light;
            }
        }

        private static bool IsHighContrastDark()
        {
            Color color = SystemColors.WindowColor;
            double luminance = (0.2126 * color.R) + (0.7152 * color.G) + (0.0722 * color.B);
            return luminance < 128;
        }

        private static void ApplySemanticPalette(ResourceDictionary resources, AppTheme theme, bool highContrast)
        {
            if (highContrast)
            {
                resources[ThemeResourceKeys.WindowBackgroundBrush] = SystemColors.WindowBrush;
                resources[ThemeResourceKeys.PageBackgroundBrush] = SystemColors.WindowBrush;
                resources[ThemeResourceKeys.ChromeBackgroundBrush] = SystemColors.WindowBrush;
                resources[ThemeResourceKeys.SurfaceBrush] = SystemColors.WindowBrush;
                resources[ThemeResourceKeys.SurfaceStrongBrush] = SystemColors.WindowBrush;
                resources[ThemeResourceKeys.BorderBrush] = SystemColors.WindowTextBrush;
                resources[ThemeResourceKeys.TextPrimaryBrush] = SystemColors.WindowTextBrush;
                resources[ThemeResourceKeys.TextSecondaryBrush] = SystemColors.WindowTextBrush;
                resources[ThemeResourceKeys.AccentBrush] = SystemColors.HighlightBrush;
                resources[ThemeResourceKeys.AccentTextBrush] = SystemColors.HighlightTextBrush;
                resources[ThemeResourceKeys.ControlBrush] = SystemColors.ControlBrush;
                resources[ThemeResourceKeys.ControlHoverBrush] = SystemColors.HighlightBrush;
                resources[ThemeResourceKeys.ControlPressedBrush] = SystemColors.HighlightBrush;
                resources[ThemeResourceKeys.TitleBarForegroundBrush] = SystemColors.ActiveCaptionTextBrush;
                resources[ThemeResourceKeys.TitleBarInactiveForegroundBrush] = SystemColors.InactiveCaptionTextBrush;
                // Glyphs sit on the window background, not the caption fill. v4.1 applied
                // its *_HC button styles regardless of activation and never dimmed them -
                // dimming a glyph is the opposite of what a contrast theme is asking for.
                resources[ThemeResourceKeys.CaptionGlyphBrush] = SystemColors.ControlTextBrush;
                resources[ThemeResourceKeys.CaptionGlyphInactiveBrush] = SystemColors.ControlTextBrush;
                resources[ThemeResourceKeys.CaptionHoverBrush] = SystemColors.HighlightBrush;
                resources[ThemeResourceKeys.CaptionHoverForegroundBrush] = SystemColors.HighlightTextBrush;
                resources[ThemeResourceKeys.CaptionPressedBrush] = SystemColors.HighlightBrush;
                resources[ThemeResourceKeys.CaptionCloseHoverBrush] = SystemColors.HighlightBrush;
                resources[ThemeResourceKeys.CaptionCloseHoverForegroundBrush] = SystemColors.HighlightTextBrush;
                resources[ThemeResourceKeys.CaptionClosePressedBrush] = SystemColors.HighlightBrush;
                // v4.1 used MiniMicaGUI.WindowFrameThickness = 4.0 here. A 1 DIP border
                // is effectively invisible once the rounded corner antialiases it, which
                // is why the contrast-theme frame appeared to be missing entirely.
                resources[ThemeResourceKeys.WindowFrameBrush] = SystemColors.ActiveCaptionBrush;
                resources[ThemeResourceKeys.WindowFrameThickness] = new Thickness(HighContrastFrameThickness);
                resources[ThemeResourceKeys.WindowFrameInactiveBrush] = SystemColors.InactiveCaptionBrush;
                // The caption band must be filled: ActiveCaptionText is defined to sit on
                // ActiveCaption, so without the fill the title renders dark-on-dark.
                resources[ThemeResourceKeys.TitleBarBackgroundBrush] = SystemColors.ActiveCaptionBrush;
                resources[ThemeResourceKeys.TitleBarInactiveBackgroundBrush] = SystemColors.InactiveCaptionBrush;
                resources[ThemeResourceKeys.BrandBorderBrush] = SystemColors.ControlTextBrush;
                resources[ThemeResourceKeys.BrandBorderThickness] = new Thickness(1);
                resources[ThemeResourceKeys.PageShadowOpacity] = 0.0;
                resources[ThemeResourceKeys.FocusBrush] = SystemColors.WindowTextBrush;
                // v4.1 forced every content string to WindowText in a contrast theme,
                // including the subtitle - the brand teal is not guaranteed to contrast.
                resources[ThemeResourceKeys.BrandInkBrush] = SystemColors.WindowTextBrush;
                // Button face/text, not Highlight: Highlight means "selected", and using
                // it for a resting button is semantically wrong even though it contrasts.
                resources[ThemeResourceKeys.BrandAccentBrush] = SystemColors.ControlBrush;
                resources[ThemeResourceKeys.BrandAccentTextBrush] = SystemColors.WindowTextBrush;
                resources[ThemeResourceKeys.BrandAccentHoverBrush] = SystemColors.HighlightBrush;
                resources[ThemeResourceKeys.BrandOnAccentBrush] = SystemColors.ControlTextBrush;
                ApplySampleIcons(resources, IsHighContrastDark());
                return;
            }

            resources[ThemeResourceKeys.WindowFrameBrush] = Brushes.Transparent;
            resources[ThemeResourceKeys.WindowFrameInactiveBrush] = Brushes.Transparent;
            resources[ThemeResourceKeys.WindowFrameThickness] = new Thickness(0);

            if (theme == AppTheme.Dark)
            {
                resources[ThemeResourceKeys.WindowBackgroundBrush] = Brush(0x20, 0x20, 0x20);
                resources[ThemeResourceKeys.PageBackgroundBrush] = Brush(0x20, 0x20, 0x20);
                resources[ThemeResourceKeys.ChromeBackgroundBrush] = Brush(0xF0, 0x20, 0x20, 0x20);
                resources[ThemeResourceKeys.SurfaceBrush] = Brush(0x2B, 0x2B, 0x2B);
                resources[ThemeResourceKeys.SurfaceStrongBrush] = Brush(0x32, 0x32, 0x32);
                resources[ThemeResourceKeys.BorderBrush] = Brush(0x48, 0xFF, 0xFF, 0xFF);
                resources[ThemeResourceKeys.TextPrimaryBrush] = Brushes.White;
                resources[ThemeResourceKeys.TextSecondaryBrush] = Brush(0xB8, 0xFF, 0xFF, 0xFF);
                resources[ThemeResourceKeys.AccentBrush] = Brush(0x00, 0x78, 0xD4);
                resources[ThemeResourceKeys.AccentTextBrush] = Brushes.White;
                resources[ThemeResourceKeys.ControlBrush] = Brush(0x35, 0x35, 0x35);
                resources[ThemeResourceKeys.ControlHoverBrush] = Brush(0x45, 0x45, 0x45);
                resources[ThemeResourceKeys.ControlPressedBrush] = Brush(0x28, 0x28, 0x28);
                // v4.1 parity: Foreground_D_Active=White, Foreground_D_Inactive=Gray,
                // HoverBackground_MaxMin_D=#363636, HoverForeground=White.
                resources[ThemeResourceKeys.TitleBarForegroundBrush] = Brushes.White;
                resources[ThemeResourceKeys.TitleBarInactiveForegroundBrush] = Brushes.Gray;
                resources[ThemeResourceKeys.CaptionGlyphBrush] = Brushes.White;
                resources[ThemeResourceKeys.CaptionGlyphInactiveBrush] = Brushes.Gray;
                resources[ThemeResourceKeys.CaptionHoverBrush] = Brush(0x36, 0x36, 0x36);
                resources[ThemeResourceKeys.CaptionHoverForegroundBrush] = Brushes.White;
                // v4.1 had no distinct pressed state; this is a slightly darker hover so
                // the press reads. Set equal to CaptionHoverBrush for strict v4.1 parity.
                resources[ThemeResourceKeys.CaptionPressedBrush] = Brush(0x2B, 0x2B, 0x2B);
                resources[ThemeResourceKeys.CaptionCloseHoverBrush] = Brush(0xC4, 0x2B, 0x1C);
                resources[ThemeResourceKeys.CaptionCloseHoverForegroundBrush] = Brushes.White;
                resources[ThemeResourceKeys.CaptionClosePressedBrush] = Brush(0xA4, 0x26, 0x2C);
                // v4.1 brand, dark: ContosoFont_D / ContosoTeal_D, hover back to Teal_L.
                resources[ThemeResourceKeys.BrandInkBrush] = Brushes.White;
                resources[ThemeResourceKeys.BrandAccentBrush] = Brush(0x14, 0xBD, 0x9B);
                resources[ThemeResourceKeys.BrandAccentTextBrush] = Brush(0x14, 0xBD, 0x9B);
                resources[ThemeResourceKeys.BrandAccentHoverBrush] = Brush(0x0F, 0x86, 0x6C);
                resources[ThemeResourceKeys.BrandOnAccentBrush] = Brushes.White;
                resources[ThemeResourceKeys.TitleBarBackgroundBrush] = Brushes.Transparent;
                resources[ThemeResourceKeys.TitleBarInactiveBackgroundBrush] = Brushes.Transparent;
                resources[ThemeResourceKeys.BrandBorderBrush] = Brushes.Transparent;
                resources[ThemeResourceKeys.BrandBorderThickness] = new Thickness(0);
                resources[ThemeResourceKeys.PageShadowOpacity] = 0.75;
                resources[ThemeResourceKeys.FocusBrush] = Brushes.White;
                ApplySampleIcons(resources, true);
                return;
            }

            resources[ThemeResourceKeys.WindowBackgroundBrush] = Brush(0xF3, 0xF3, 0xF3);
            resources[ThemeResourceKeys.PageBackgroundBrush] = Brush(0xF3, 0xF3, 0xF3);
            resources[ThemeResourceKeys.ChromeBackgroundBrush] = Brush(0xF2, 0xF3, 0xF3, 0xF3);
            resources[ThemeResourceKeys.SurfaceBrush] = Brush(0xFF, 0xFF, 0xFF);
            resources[ThemeResourceKeys.SurfaceStrongBrush] = Brush(0xFF, 0xFF, 0xFF);
            resources[ThemeResourceKeys.BorderBrush] = Brush(0x28, 0x00, 0x00, 0x00);
            resources[ThemeResourceKeys.TextPrimaryBrush] = Brush(0x18, 0x18, 0x18);
            resources[ThemeResourceKeys.TextSecondaryBrush] = Brush(0x99, 0x00, 0x00, 0x00);
            resources[ThemeResourceKeys.AccentBrush] = Brush(0x00, 0x67, 0xC0);
            resources[ThemeResourceKeys.AccentTextBrush] = Brushes.White;
            resources[ThemeResourceKeys.ControlBrush] = Brush(0xFA, 0xFA, 0xFA);
            resources[ThemeResourceKeys.ControlHoverBrush] = Brush(0xE5, 0xE5, 0xE5);
            resources[ThemeResourceKeys.ControlPressedBrush] = Brush(0xD8, 0xD8, 0xD8);
            // v4.1 parity: Foreground_L_Active=Black, Foreground_L_Inactive=Silver,
            // HoverBackground_MaxMin_L=#DBDBDB, HoverForeground_MaxMin_L=Black.
            resources[ThemeResourceKeys.TitleBarForegroundBrush] = Brushes.Black;
            resources[ThemeResourceKeys.TitleBarInactiveForegroundBrush] = Brushes.Silver;
            resources[ThemeResourceKeys.CaptionGlyphBrush] = Brushes.Black;
            resources[ThemeResourceKeys.CaptionGlyphInactiveBrush] = Brushes.Silver;
            resources[ThemeResourceKeys.CaptionHoverBrush] = Brush(0xDB, 0xDB, 0xDB);
            resources[ThemeResourceKeys.CaptionHoverForegroundBrush] = Brushes.Black;
            // v4.1 had no distinct pressed state; this is a slightly darker hover so the
            // press reads. Set equal to CaptionHoverBrush for strict v4.1 parity.
            resources[ThemeResourceKeys.CaptionPressedBrush] = Brush(0xCF, 0xCF, 0xCF);
            resources[ThemeResourceKeys.CaptionCloseHoverBrush] = Brush(0xC4, 0x2B, 0x1C);
            resources[ThemeResourceKeys.CaptionCloseHoverForegroundBrush] = Brushes.White;
            resources[ThemeResourceKeys.CaptionClosePressedBrush] = Brush(0xA4, 0x26, 0x2C);
            // v4.1 brand, light: ContosoFont_L #001D2F / ContosoTeal_L #0F866C,
            // hover to Teal_D #14BD9B.
            resources[ThemeResourceKeys.BrandInkBrush] = Brush(0x00, 0x1D, 0x2F);
            resources[ThemeResourceKeys.BrandAccentBrush] = Brush(0x0F, 0x86, 0x6C);
            resources[ThemeResourceKeys.BrandAccentTextBrush] = Brush(0x0F, 0x86, 0x6C);
            resources[ThemeResourceKeys.BrandAccentHoverBrush] = Brush(0x14, 0xBD, 0x9B);
            resources[ThemeResourceKeys.BrandOnAccentBrush] = Brushes.White;
            resources[ThemeResourceKeys.TitleBarBackgroundBrush] = Brushes.Transparent;
            resources[ThemeResourceKeys.TitleBarInactiveBackgroundBrush] = Brushes.Transparent;
            resources[ThemeResourceKeys.BrandBorderBrush] = Brushes.Transparent;
            resources[ThemeResourceKeys.BrandBorderThickness] = new Thickness(0);
            resources[ThemeResourceKeys.PageShadowOpacity] = 0.25;
            resources[ThemeResourceKeys.FocusBrush] = Brushes.Black;
            ApplySampleIcons(resources, false);
        }

        /// <summary>
        /// Swaps the sample page's bullet icons for the light or dark artwork. v4.1 did
        /// this by reassigning Image.Source in code-behind on every theme change; routing
        /// it through the resource dictionary instead means DynamicResource re-resolves
        /// automatically and no view needs a theme handler.
        ///
        /// This is the one place the shell references sample branding. A derived app
        /// either replaces the three asset pairs or deletes this method along with the
        /// bullet rows in SamplePage.xaml.
        /// </summary>
        private static void ApplySampleIcons(ResourceDictionary resources, bool dark)
        {
            string suffix = dark ? "_d" : "_l";
            resources[ThemeResourceKeys.SampleBullet1] = LoadIcon("bullet1" + suffix);
            resources[ThemeResourceKeys.SampleBullet2] = LoadIcon("bullet2" + suffix);
            resources[ThemeResourceKeys.SampleBullet3] = LoadIcon("bullet3" + suffix);
        }

        private static ImageSource LoadIcon(string name)
        {
            try
            {
                BitmapImage image = new BitmapImage(
                    new Uri("pack://application:,,,/Resources/Branding/" + name + ".png", UriKind.Absolute));
                image.Freeze();
                return image;
            }
            catch
            {
                // A missing asset must not take the window down; the Image simply
                // renders empty and the row still lays out.
                return null;
            }
        }

        private static SolidColorBrush Brush(byte r, byte g, byte b)
        {
            return Freeze(new SolidColorBrush(Color.FromRgb(r, g, b)));
        }

        private static SolidColorBrush Brush(byte a, byte r, byte g, byte b)
        {
            return Freeze(new SolidColorBrush(Color.FromArgb(a, r, g, b)));
        }

        private static SolidColorBrush Freeze(SolidColorBrush brush)
        {
            brush.Freeze();
            return brush;
        }
    }
}
