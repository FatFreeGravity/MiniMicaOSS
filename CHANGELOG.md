# Changelog

## 5.0.0

First release of the v5 line. Rebuilt on the v4.1 visual design with the
plumbing modernized. Not source-compatible with v4.1, see
[migrating from v4.1](docs/migrating-from-v41.md).

### Added
- **Windows 10 1903+ runs.** The minimum is build 18362, where .NET Framework 4.8 first
  shipped in the box. Verified on Windows 10 20H1 (19041): all functions work, without
  Mica, rounded corners or the Snap Layouts flyout. Windows 11 22H2+ remains the
  designed-for target.
- **Icon font fallback.** Segoe Fluent Icons is Windows 11 only, so every use of it now
  names Segoe MDL2 Assets after it. WPF resolves a font list per glyph, so Windows 11
  renders exactly as before and Windows 10 gets a font that exists.
- **Keyboard and Narrator support.** Tab reaches the caption buttons, the pan
  chevrons and the page text; a solid 2px theme-colored focus ring replaces WPF's
  near-invisible default. Text focusability is set once in the shared styles rather
  than repeated per element as in v4.1.
- **Fork without the CLI.** `docs/forking-in-visual-studio.md` covers the Export
  Template and copy-rename routes. Renaming no longer depends on string literals:
  `Strings` derives its resource base name from its namespace and
  `AppOptions.DisplayName` reads the assembly `Product` attribute.
- **Snap Layouts over custom chrome.** `WM_NCHITTEST` answers `HTMAXBUTTON`, so
  the Windows 11 flyout appears over the custom maximize button.
- **Windows contrast theme support.** Filled caption band, 4 DIP window outline,
  system-color mapping throughout, live switching without restart.
- **Developer language override.** Open Settings holding Ctrl+Shift for a culture
  picker that forces a UI language, for localization testing.
- **Per-culture layout metrics.** Font sizes and wrap width come from `metric_*`
  rows in the translation worksheet, so a language that overflows the fixed canvas
  is fixed by editing a spreadsheet cell.
- **Offline verification suite** (`tools/verify/verify.py`), resource keys,
  XAML/C# wiring, theme branch coverage, brush roles, pixel contracts, template
  rename. CI gates the Windows build on it.
- Cross-platform localization generator (`generate_resx.py`) alongside the
  PowerShell one.
- 14 satellite cultures committed by default; 22 / 25 / All one flag away.

### Changed
- **Identity and version moved to `Properties/AssemblyInfo.cs`** with
  `GenerateAssemblyInfo=false`, matching the classic WPF layout. `AssemblyVersion` and
  `AssemblyFileVersion` are `5.0.26242.0`; company and copyright come from v4.1.
- `AppVersion.Display` reports Major.Minor.Build, so the Settings dialog shows
  `5.0.26242`. It previously dropped the build number that `{M.m.build}` names.
- **Custom title bar only.** The v1.0 native-title-bar prototype is gone.
  ([ADR 0005](docs/decisions/0005-custom-chrome.md))
- **Fixed design canvas** with horizontal panning replaces page scrolling.
  ([ADR 0006](docs/decisions/0006-fixed-page-host.md))
- Real `WindowState` instead of v4.x pseudo-maximize; Windows keeps restore bounds.
- Settings persist to `%LOCALAPPDATA%\OEM\MiniMica\<App>\app.config` in v4.1's
  `appSettings` format, readable by derived apps via `ConfigurationManager`.
- Theme values are resources filled by `ThemeManager`, not code assigning brushes
  per control.
- Typography moved out of views into `Resources/Styles.xaml`.
  ([ADR 0007](docs/decisions/0007-branding-and-metrics.md))
- Solution converted to `MiniMica.slnx`.
- Caption tooltips and automation names localized in all 25 cultures using
  standard Windows terminology.
- Hero image re-encoded 1,111,401 → 247,909 bytes with no visible difference.

### Fixed
- Caption glyph rendering: reverted to `Segoe MDL2 Assets` @ 10; Segoe Fluent
  Icons shifted the glyphs off pixel boundaries.
- Close button hover now actually turns red. The hover trigger had been placed
  where a derived style could not override it.
- Hovering a caption button on an inactive window restores full contrast.
- Theme changes apply immediately when the window is not focused.
- Settings dialog restored to v4.1's 480 × 300; 600 × 375 came from reading a
  125 % DPI screenshot as DIPs.
- Removed the accent color from the Settings dialog's radio buttons and
  checkboxes; stock WPF controls again.
- Sample page no longer clips its own text; v4.1 layout metrics restored.
- Rounded corners no longer leak desktop when maximized.

### Removed
- System/native title bar mode.
- Whole-page vertical scrolling and any page scaling.
- Stale internal docs (private-material review, legacy migration notes).
