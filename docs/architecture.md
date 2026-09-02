# Architecture

```
App.xaml.cs
  ├─ WindowsVersion guard ......... refuse to start below Win10 20H1 (19041)
  ├─ SettingsManager .............. load %LOCALAPPDATA%\OEM\MiniMica\<App>\app.config
  ├─ LocalizationManager .......... apply culture, then per-culture metrics
  ├─ ThemeManager ................. fill the semantic palette
  └─ MainWindow (MiniMicaWindow)
       ├─ WindowChrome.xaml ....... caption template
       ├─ CustomChromeController .. WM_GETMINMAXINFO, WM_NCHITTEST, snap
       ├─ WindowAppearanceController  DWM backdrop, live theme refresh
       └─ FixedPageHost ........... fixed canvas + horizontal panning
            └─ SamplePage ......... your content
```

**Startup order matters.** Settings load before localization (they carry the
language override); the culture is applied before any `FrameworkElement` exists
(XAML resolves localized strings as it loads); the palette is filled before the
first window paints.

---

## Shell

`MiniMicaWindow` is a `Window` with `WindowStyle=None` plus WPF `WindowChrome`.
It owns caption *visuals*; Windows owns window *state*. See
[ADR 0005](decisions/0005-custom-chrome.md).

| Type | Responsibility |
|---|---|
| `MiniMicaWindow` | template parts, caption commands, system menu, `Alt+Space` |
| `CustomChromeController` | native messages: maximized geometry, min track size, `HTMAXBUTTON` for Snap Layouts, non-client mouse for the maximize button |
| `WindowAppearanceController` | DWM backdrop and corners; re-applies the palette on `WM_SETTINGCHANGE` / `WM_THEMECHANGED` and on a contrast-theme switch |

Useful properties: `Backdrop`, `CornerPreference`, `TitleBarHeight`,
`ShowSettingsButton`, `ShowMinimizeButton`, `ShowMaximizeButton`,
`IsSnapLayoutEnabled`.

---

## Theming

`ThemeManager.Apply(ResourceDictionary, AppTheme)` writes every key in
`ThemeResourceKeys` into the dictionary. Controls bind `DynamicResource`, so a
theme change needs no code, the resources re-resolve.

Three branches: **light**, **dark**, **contrast**. Contrast maps to
`SystemColors`, never to literals.

Two invariants, both enforced by `tools/verify/verify.py`:

- every key is assigned in **all three** branches. A key set only in light keeps
  a stale value elsewhere
- **fill-role and text-role brushes are separate keys**, contrast themes collapse
  colors onto the same value, so a fill brush used as text disappears

`Resources/Styles.xaml` holds a designer-time fallback for every key so the XAML
designer renders.

---

## Fixed page

`FixedPageHost` is a `ContentControl` wrapping a `ScrollViewer` with the
horizontal bar hidden and two `RepeatButton`s over it. It sets the window's
minimum size from `DesignHeight` + title bar while `EnforceWindowConstraints` is
true. See [ADR 0006](decisions/0006-fixed-page-host.md).

---

## Settings

| Type | Responsibility |
|---|---|
| `AppSettings` | the four values: theme, notifications, telemetry, language |
| `SettingsManager` | process-wide state, change event, persistence |
| `SettingsStore` | reads/writes the per-user `app.config` via `ConfigurationManager` |
| `SettingsWindow` | the dialog, itself a `MiniMicaWindow` with only Close |

MiniMica provides the **switches**, not the implementations. A derived app's
notification or telemetry code must consult
`SettingsManager.Current.NotificationsEnabled` / `.TelemetryEnabled`.

File encoding is inherited from v4.1 and is a compatibility contract:
`appearance` is `0=Dark, 1=Light, 2=Automatic`, **not** the `AppTheme` enum
order. See [localization.md](localization.md) for `language`.

---

## Localization

`worksheet.csv` → generator → `Strings.<culture>.resx` → satellite assemblies.
`Strings.Get`/`Expand` wrap `ResourceManager`; `{i18n:Loc Key=…}` is the XAML
markup extension, resolved at load time. Full pipeline in
[localization.md](localization.md).

---

## Platform layer

| Namespace | Contents |
|---|---|
| `Platform.Windows.Dwm` | `DWMWA_SYSTEMBACKDROP_TYPE`, corner preference, dark title bar |
| `Platform.Windows.Theme` | `AppTheme`, `ThemeManager`, `ThemeResourceKeys` |
| `Platform.Windows.Windowing` | monitor info, DPI, work-area placement |
| `Platform.Windows` | `WindowsVersion` build guard |

Identity, version and copyright live in `Properties/AssemblyInfo.cs`, not in MSBuild
properties. `MiniMicaApp.csproj` sets `GenerateAssemblyInfo=false` so the two do not
collide; `verify.py precompile` fails if that opt-out is removed or if the same
attribute is declared in both places.

Only documented DWM attributes available across the supported baseline are used.
