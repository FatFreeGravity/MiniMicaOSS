# Customization

MiniMica is optimized for source ownership. Modify it instead of working around it.

## Change product identity

When creating through the template, use `-n`:

```powershell
dotnet new minimica -n Contoso.Desktop
```

The .NET template engine replaces `MiniMicaApp` in filenames and file contents.

If you cloned the repository instead, a normal solution-wide rename of `MiniMicaApp` is safe because platform code does not build registry paths, mutex names, task names, or analytics IDs from scattered hard-coded strings.

## Change the default theme

Template creation:

```powershell
dotnet new minimica -n Contoso.Desktop --theme Dark
```

At runtime:

```csharp
window.ThemePreference = AppTheme.Dark;
```

To change the default in a cloned source tree, update `AppOptions.DefaultTheme` or replace the sentinel strategy with a normal constant after you no longer need the project to be template source.

## Change the backdrop

```csharp
window.Backdrop = BackdropKind.Mica;
window.Backdrop = BackdropKind.Acrylic;
window.Backdrop = BackdropKind.Tabbed;
window.Backdrop = BackdropKind.None;
```

Always keep a usable opaque fallback. `IsBackdropActive` tells the view whether DWM actually accepted the backdrop.

## Change colors

Edit the palettes in `ThemeManager.ApplySemanticPalette`. UI should use these resource keys:

```text
MiniMica.WindowBackgroundBrush
MiniMica.SurfaceBrush
MiniMica.SurfaceStrongBrush
MiniMica.BorderBrush
MiniMica.TextPrimaryBrush
MiniMica.TextSecondaryBrush
MiniMica.AccentBrush
MiniMica.AccentTextBrush
MiniMica.ControlBrush
MiniMica.ControlHoverBrush
MiniMica.ControlPressedBrush
```

Because they are dynamic resources, open windows update when the palette changes.

## Add application services

Add product services to a new `Services` folder. If you use dependency injection, configure it in `App.xaml.cs`, but keep in mind that runtime packages increase the distribution payload.

Do not put WebView2, analytics, API clients, installers, or product business logic in `MiniMicaWindow` or `Platform/Windows`.

## Remove the live appearance test

Delete the last card in `MainWindow.xaml`, the six button handlers in `MainWindow.xaml.cs`, and keep `UpdateDiagnostics` only if you find it useful.

No platform code depends on the sample UI.


## Localize product strings

Move user-visible strings into `Localization/Strings.resx` and reference them with `{i18n:Loc Key=...}` in XAML. The base template contains neutral English only. Use `tools/localization/generate-resx.ps1` to add selected cultures; see [Localization](features/localization.md).
