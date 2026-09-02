# Migrating from v4.1

v5 keeps the v4.1 *look* and replaces the *plumbing*. Expect to port your page and
assets, not your shell.

## What transfers unchanged

| Thing | Note |
|---|---|
| Branding assets | `res/*` → `Resources/Branding/`, same file names |
| Settings file | same path and format; existing user preferences are read |
| Translations | worksheet columns map 1:1: but see culture naming below |
| Layout metrics | 960 × 640 canvas, 30 DIP title bar, 45 DIP buttons |
| Brand colors | `ContosoFont_*` / `ContosoTeal_*` → `MiniMica.Brand*` |

## What changed

| v4.1 | v5 |
|---|---|
| `MiniMica.MiniMicaWindow` (XAML window) | `MiniMicaApp.Shell.MiniMicaWindow` (templated) |
| `MainControl` / `DialogControl` user controls | `Views/SamplePage.xaml`, `Settings/SettingsWindow.xaml` |
| `UpdateTheme(bool)` per control | `ThemeManager` fills resources; controls bind `DynamicResource` |
| `_isPseudoMaximized` state | real `WindowState` |
| Styles swapped in code (`CloseButton_L_Activated`) | one style, `Style.Triggers` + theme resources |
| `Global.appSettings` struct | `SettingsManager.Current` |
| `MiniMicaConfig` | `SettingsStore` (same file, same keys) |
| Inline fonts/colors in XAML | named styles in `Resources/Styles.xaml` |
| Per-language sizes hardcoded | `metric_*` rows in the worksheet |

## Steps

1. **Generate a v5 app.** `dotnet new minimica -n YourApp`. Do not copy the v5
   shell into a v4.1 tree.
2. **Copy assets** into `Resources/Branding/`, keeping names. Re-encode large
   images, see [branding.md](branding.md#step-2--assets).
3. **Move brand colors** into the `MiniMica.Brand*` keys in `Styles.xaml` **and**
   all three `ThemeManager` branches.
4. **Port typography** into the named styles. Do not put fonts back in views.
5. **Rebuild the page** in `SamplePage.xaml` using styles and `{i18n:Loc}`.
6. **Import translations.** Paste v4.1 columns into `worksheet.csv`, then map
   culture names:

   | v4.1 | v5 | Watch out |
   |---|---|---|
   | `zh` | `zh-TW` | v4.1 neutral `zh` is **Traditional** |
   | `zh-CN` | `zh-CN` | Simplified |
   | `pt` | `pt-BR` | `pt-PT` is separate and different |
   | `es` | `es-MX` | `es-ES` is separate |
   | `nb` | `nb-NO` | |

   Also rewrite the product placeholder: v4.1 used `{Contoso}`, v5 uses
   `{ProductName}`. Leaving it produces a literal "{Contoso}" in 24 languages.

7. **Regenerate and verify.**

   ```bash
   python3 tools/localization/generate_resx.py --tier 14 --clean
   python3 tools/verify/verify.py
   ```

## Behavior differences to expect

- Narrow windows **pan** instead of scrolling.
- Maximize is real, so restore bounds and snap follow Windows.
- Hovering maximize shows the Snap Layouts flyout.
- Contrast themes look materially different, and correct.
- A language change asks to restart; XAML resolves strings at load time.
