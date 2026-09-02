# Themes and accessibility

## Three themes, one palette

Nothing names a color. Controls resolve semantic keys via `DynamicResource`;
`ThemeManager` fills them for light, dark and Windows contrast themes. A theme
change needs no code.

```
Appearance = Automatic  →  follow Windows (registry AppsUseLightTheme)
             Light / Dark →  forced
Contrast theme active   →  overrides everything, maps to SystemColors
```

Set through the Settings dialog; persisted as `appearance` in the per-user
`app.config` (`0=Dark, 1=Light, 2=Automatic`).

## Two invariants

Both enforced by `tools/verify/verify.py`, because both have shipped as bugs:

1. **Every key is assigned in all three branches.** A key set only in the light
   branch keeps a stale value in dark and contrast. This is how the caption bar
   once became unreadable.
2. **Fill-role and text-role brushes are separate keys.** Contrast themes collapse
   many colors onto one value, so a fill brush used as `Foreground` is invisible
   even though it looked correct in light and dark, this is how the sample
   subtitle once vanished under every contrast theme.

## Contrast themes

| Surface | Mapping |
|---|---|
| Window / page background | `WindowBrush` |
| Body text, headings, subtitle | `WindowTextBrush` |
| Caption band | `ActiveCaption` / `InactiveCaption` |
| Title text | `ActiveCaptionText` / `InactiveCaptionText` |
| Caption glyphs | `ControlText`, never dimmed |
| Hover / selection | `Highlight` / `HighlightText` |
| Action button | `ControlBrush` + `ControlText` + visible outline |
| Window outline | `ActiveCaption`, 4 DIP, drawn as an overlay |
| Drop shadow | disabled |
| Backdrop | disabled |

Brand colors are **not** used: the teal is not guaranteed to contrast. Raster
icons switch to the artwork matching the theme's polarity.

Live switching works: `WindowAppearanceController` listens for
`WM_SETTINGCHANGE` / `WM_THEMECHANGED` and for `SystemParameters.HighContrast`.

## Keyboard and automation

| Concern | Status |
|---|---|
| `Alt+Space` system menu | supported |
| Caption button automation names | localized via `titlebar_*` |
| Pan button automation names | localized via `pan_*` |
| Settings dialog | fully keyboard reachable, stock controls |
| Caption buttons focusable | **no**: matches Windows title bar semantics |

Caption buttons are `Focusable="False"` deliberately: a Windows title bar is not
in the tab order. They remain reachable through UI Automation and the system menu.

## Known gap

The primary action button has no keyboard focus visual. v4.1 had none either;
adding one is a small change to `MiniMica.ActionButtonStyle` and is worth doing
before an accessibility review.
