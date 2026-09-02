# Branding a derived app

A procedure for turning the MiniMica sample into your product. Work top to
bottom; each step is independent and verifiable.

**Branding in MiniMica means five things:** identity, assets, colors,
typography, copy. Nothing else needs to change to ship a different-looking app.

Views never name a font, size or color. They name a *style*. That is what makes
this a bounded edit instead of a search across the tree.

---

## Step 0: Generate the project

```powershell
dotnet new install .\artifacts\MiniMica.Templates.5.0.0.nupkg
dotnet new minimica -n Contoso
```

Renaming is handled for you: namespaces, assembly name, `x:Class`, the
`ResourceManager` base name, the app manifest, and the settings path all become
`Contoso`. Verify with:

```bash
python3 tools/verify/verify.py rename
```

---

## Step 1: Identity

**`src/<YourApp>/Configuration/AppOptions.cs`** is the knob file. Most forks only
touch this and the assets.

| Field | Meaning |
|---|---|
| `DisplayNameOverride` | force a friendly product name; leave empty to follow `AssemblyProduct` |
| `DesignWidth` / `DesignHeight` | your canvas size |
| `MinimumViewportWidth` | narrowest window before panning (Snap needs ≤ 500) |
| `TitleBarHeight` | 30 matches Windows 11; change only with reason |
| `DefaultNotificationsEnabled` / `DefaultTelemetryEnabled` | first-run defaults |
| `ConfigVendorFolder` / `ConfigFamilyFolder` | `%LOCALAPPDATA%\OEM\MiniMica\<App>\` |

**`<YourApp>/Properties/AssemblyInfo.cs`** holds identity, version and copyright,
the way a classic WPF project does. Edit `AssemblyTitle`, `AssemblyCompany`,
`AssemblyProduct`, `AssemblyCopyright` and the two version attributes.

`AppOptions.DisplayName` reads `AssemblyProduct`, so the title bar follows it
automatically. The csproj sets `GenerateAssemblyInfo=false`, so do not also put
version or product properties there: they would be silently ignored.

---

## Step 2: Assets

Replace files in **`src/<YourApp>/Resources/Branding/`**, keeping the names:

| File | Used for | Notes |
|---|---|---|
| `app.ico` | exe icon, title bar | include 16/32/48/256 |
| `app.png` | title bar icon | 32×32, drawn at 16 DIP |
| `hero.jpg` | page artwork | see budget note below |
| `bullet1_l.png` / `bullet1_d.png` | feature icon 1 | `_l` for light, `_d` for dark |
| `bullet2_*`, `bullet3_*` | feature icons 2–3 | 30 DIP, transparent PNG |

Adding a *new* asset needs a `<Resource Include="…" />` entry in the `.csproj`.
Miss it and the pack URI throws at runtime, `verify.py styles` catches it.

**Image budget.** Payload target is ~1 MB; CI fails over 1.25 MiB. Size raster
art for its largest rendered size at 200 % DPI, not its original resolution.

> The sample hero renders at 480 DIP, so 1100 px covers scaling past 200 %.
> Re-encoding the original 1170 × 866 photo at quality 88 took it from
> **1,111,401 → 247,909 bytes** with no visible difference. Do the same.

Theme-paired assets are swapped by `ThemeManager.ApplySampleIcons`. Rename or
delete those keys when you replace the sample rows.

---

## Step 3: Colors

Two palettes, deliberately separate:

- **Neutral** (`MiniMica.Text*`, `Control*`, `Surface*`, `Caption*`), shell
  chrome. Leave alone unless you are changing the window look itself.
- **Brand** (`MiniMica.Brand*`), your product. This is what you edit.

| Key | Role |
|---|---|
| `BrandInkBrush` | body and heading text |
| `BrandAccentTextBrush` | accent used **as text** (subtitle) |
| `BrandAccentBrush` | accent used **as fill** (button background) |
| `BrandAccentHoverBrush` | fill on hover |
| `BrandOnAccentBrush` | text on top of the accent fill |

Edit in **two places, both required**:

1. `Resources/Styles.xaml` for the designer-time fallback
2. `Platform/Windows/Theme/ThemeManager.cs` for the light, dark **and** contrast branches

> **Keep the text and fill roles separate.** Under a contrast theme the fill role
> becomes button-face, which equals the window background in most contrast
> themes. A text element bound to the fill brush becomes invisible. This is a real
> bug that shipped once. `verify.py styles` now fails on it.

For contrast themes, map brand colors to `SystemColors`, never to a literal.

```bash
python3 tools/verify/verify.py precompile styles   # branch coverage + role check
```

---

## Step 4: Typography

All of it lives in **`Resources/Styles.xaml`**:

| Style | Applies to |
|---|---|
| `MiniMica.PageTitleStyle` | page title |
| `MiniMica.PageSubtitleStyle` | subtitle |
| `MiniMica.FeatureHeadingStyle` | feature headings |
| `MiniMica.FeatureBodyStyle` | feature body text |
| `MiniMica.ActionButtonStyle` | primary call to action (pill) |
| `MiniMica.PageNoteStyle` | small print |

Font sizes are **not** literals in these styles, they resolve
`MiniMica.*FontSize` resources so they can vary per culture (see
[localization.md](localization.md#step-6--fix-overflow-without-touching-xaml)).

Two ways to restyle:

- **Named styles** (what the sample uses). Each view states its role. Clearer.
- **Implicit styles**: drop `x:Key`, keep `TargetType`, and the rule applies to
  every control of that type in scope. Good for blanket defaults.

The action button is a pill because `CornerRadius="25"` meets `Height="50"`.
Change both together.

> **Do not put fonts or colors back into views.** `verify.py styles` fails on any
> `FontFamily`, `FontSize`, `FontWeight` or `Foreground` attribute under `Views/`.

---

## Step 5: Copy and layout

Text comes from `tools/localization/worksheet.csv`, never from XAML, see
[localization.md](localization.md). Even an English-only app benefits: one file
holds every string.

Rewriting `Views/SamplePage.xaml`:

- keep `Width`/`Height` equal to your `DesignWidth`/`DesignHeight`
- reference styles by name; set no typography
- get text from `{i18n:Loc Key=…}`
- get theme-paired images from `{DynamicResource …}`
- delete the sample's bottom-right diagnostic note

---

## Step 6: Verify

```bash
python3 tools/verify/verify.py          # all static checks
.\scripts\build-and-report.ps1          # Windows build + payload budget
```

Then walk `docs/v5-test-protocol.md`, at minimum light, dark and one contrast
theme, and one non-100 % DPI setting.

---

## Checklist

```
[ ] AppOptions: DisplayName, canvas size, config folders
[ ] AssemblyInfo.cs: title, company, product, copyright, version
[ ] Assets replaced; new ones added as <Resource>
[ ] Hero re-encoded for its rendered size
[ ] Brand colors in Styles.xaml AND all three ThemeManager branches
[ ] Text-role and fill-role brushes kept separate
[ ] Typography in Styles.xaml only; views clean
[ ] Strings in worksheet.csv; satellites regenerated
[ ] verify.py passes
[ ] Release payload within budget
[ ] Light / dark / contrast checked
```

---

## Things not to change without reason

| Thing | Why it is the way it is |
|---|---|
| 30 DIP title bar, 45 DIP caption buttons | matched pixel-wise to Windows 11 |
| `Segoe MDL2 Assets` @ 10 for caption glyphs | Segoe Fluent Icons shifts these glyphs off pixel boundaries |
| Settings dialog 480 × 300 | measured layout; the reference screenshot is 125 % DPI |
| Stock WPF radio buttons and checkboxes | familiar, and they avoid an accent color in a neutral dialog |
| 4 DIP contrast frame | thinner disappears into the rounded corner |

`verify.py chrome` enforces these.
