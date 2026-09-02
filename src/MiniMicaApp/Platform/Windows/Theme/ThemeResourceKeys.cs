namespace MiniMicaApp.Platform.Windows.Theme
{
    internal static class ThemeResourceKeys
    {
        internal const string WindowBackgroundBrush = "MiniMica.WindowBackgroundBrush";
        internal const string PageBackgroundBrush = "MiniMica.PageBackgroundBrush";
        internal const string ChromeBackgroundBrush = "MiniMica.ChromeBackgroundBrush";
        internal const string SurfaceBrush = "MiniMica.SurfaceBrush";
        internal const string SurfaceStrongBrush = "MiniMica.SurfaceStrongBrush";
        internal const string BorderBrush = "MiniMica.BorderBrush";
        internal const string TextPrimaryBrush = "MiniMica.TextPrimaryBrush";
        internal const string TextSecondaryBrush = "MiniMica.TextSecondaryBrush";
        internal const string AccentBrush = "MiniMica.AccentBrush";
        internal const string AccentTextBrush = "MiniMica.AccentTextBrush";
        internal const string ControlBrush = "MiniMica.ControlBrush";
        internal const string ControlHoverBrush = "MiniMica.ControlHoverBrush";
        internal const string ControlPressedBrush = "MiniMica.ControlPressedBrush";
        // Caption chrome. These values are ported verbatim from MiniMica v4.1, which
        // tuned them against the Windows 11 Settings app. Do not "improve" them casually.
        // Title TEXT sits on the caption fill, so under a contrast theme it must use the
        // ActiveCaptionText/ActiveCaption pair.
        internal const string TitleBarForegroundBrush = "MiniMica.TitleBarForegroundBrush";
        internal const string TitleBarInactiveForegroundBrush = "MiniMica.TitleBarInactiveForegroundBrush";

        // Caption BUTTON glyphs are a separate color because they do not sit on the
        // caption fill - v4.1 stops that fill at the button columns, leaving the glyphs on
        // the plain window background. Under a contrast theme they therefore need
        // ControlText, not ActiveCaptionText, which is only legible on the caption color.
        internal const string CaptionGlyphBrush = "MiniMica.CaptionGlyphBrush";
        internal const string CaptionGlyphInactiveBrush = "MiniMica.CaptionGlyphInactiveBrush";
        internal const string CaptionHoverBrush = "MiniMica.CaptionHoverBrush";
        internal const string CaptionHoverForegroundBrush = "MiniMica.CaptionHoverForegroundBrush";
        internal const string CaptionPressedBrush = "MiniMica.CaptionPressedBrush";
        internal const string CaptionCloseHoverBrush = "MiniMica.CaptionCloseHoverBrush";
        internal const string CaptionCloseHoverForegroundBrush = "MiniMica.CaptionCloseHoverForegroundBrush";
        internal const string CaptionClosePressedBrush = "MiniMica.CaptionClosePressedBrush";
        internal const string WindowFrameBrush = "MiniMica.WindowFrameBrush";
        internal const string WindowFrameThickness = "MiniMica.WindowFrameThickness";
        internal const string WindowFrameInactiveBrush = "MiniMica.WindowFrameInactiveBrush";

        // Title bar fill. Transparent in light/dark so the Mica backdrop shows through;
        // in a Windows contrast theme it becomes the system caption color, which is what
        // makes ActiveCaptionText legible - that pair only works together.
        internal const string TitleBarBackgroundBrush = "MiniMica.TitleBarBackgroundBrush";
        internal const string TitleBarInactiveBackgroundBrush = "MiniMica.TitleBarInactiveBackgroundBrush";

        // Outline for the primary action button. Invisible outside contrast themes, where
        // a filled shape carries the meaning; a contrast theme needs a real border.
        internal const string BrandBorderBrush = "MiniMica.BrandBorderBrush";
        internal const string BrandBorderThickness = "MiniMica.BrandBorderThickness";

        // Page drop shadow opacity. Zero under a contrast theme - a soft shadow is noise
        // when the palette is deliberately flat.
        internal const string PageShadowOpacity = "MiniMica.PageShadowOpacity";

        // Keyboard focus ring. Must contrast against whatever it outlines, so it tracks
        // the primary text color rather than an accent.
        internal const string FocusBrush = "MiniMica.FocusBrush";

        // Brand palette for page content. Separate from the neutral Text*/Control*
        // palette above so a fork can restyle its product identity without touching
        // shell chrome. Values are the v4.1 Contoso brand.
        internal const string BrandInkBrush = "MiniMica.BrandInkBrush";
        // Accent as a FILL (the action button). Under a contrast theme this becomes the
        // system button face, which in most contrast themes equals the window background.
        internal const string BrandAccentBrush = "MiniMica.BrandAccentBrush";

        // Accent as TEXT (the subtitle). It must NOT share the fill brush: button face on
        // a window background is invisible. v4.1 forced accent text to WindowText under a
        // contrast theme, and that is what this key does.
        internal const string BrandAccentTextBrush = "MiniMica.BrandAccentTextBrush";
        internal const string BrandAccentHoverBrush = "MiniMica.BrandAccentHoverBrush";
        internal const string BrandOnAccentBrush = "MiniMica.BrandOnAccentBrush";

        // Sample page bullet icons. Raster pairs, one per theme, following v4.1.
        // A derived app replaces these three assets or deletes the rows that use them.
        internal const string SampleBullet1 = "MiniMica.SampleBullet1";
        internal const string SampleBullet2 = "MiniMica.SampleBullet2";
        internal const string SampleBullet3 = "MiniMica.SampleBullet3";
    }
}
