# Mica and backdrops

MiniMica asks DWM for a system backdrop through
`DWMWA_SYSTEMBACKDROP_TYPE`, documented and available across the supported
baseline (Windows 11 22H2+).

```xml
<shell:MiniMicaWindow Backdrop="Mica" CornerPreference="Round">
```

| `BackdropKind` | Use |
|---|---|
| `Mica` | main windows: the default |
| `Acrylic` | transient surfaces |
| `Tabbed` | tabbed Mica variant |
| `Auto` | let Windows choose |
| `None` | disable |

Set the default for a fork in `AppOptions.DefaultBackdrop`, or per window.
`MiniMicaWindow.IsBackdropActive` reports whether DWM accepted the request.

## How it interacts with the custom title bar

Because the caption is drawn by MiniMica, the backdrop shows through it: the
caption fill is transparent in light and dark. That is why there is a
`TitleBarBackgroundBrush` at all. It stays transparent so Mica is visible, and
only becomes opaque under a contrast theme, where a filled band is required for
`ActiveCaptionText` to be legible.

`WindowAppearanceController` still calls `ApplyDarkTitleBar`. With fully custom
chrome this affects only hidden non-client surfaces and the system menu, so it is
kept.

## Contrast themes disable it

`SystemParameters.HighContrast` forces `BackdropKind.None`, a translucent
material defeats the point of a contrast theme.

## Windows 10

`DWMWA_SYSTEMBACKDROP_TYPE` needs Windows 11 22H2, so `ApplyBackdrop` returns false
and the window renders with `MiniMica.WindowBackgroundBrush`. Rounded corners need
Windows 11 21H2. Both are checked through `WindowsVersion`, so nothing throws and
nothing blocks startup.

## Requirements

- Window background must be transparent for the material to show
- `AllowsTransparency` must stay **false**; setting it true disables the backdrop
  and the drop shadow
- Backdrops are a compositor courtesy: if DWM declines, the window still renders
  with `MiniMica.WindowBackgroundBrush`
