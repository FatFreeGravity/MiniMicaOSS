# Project structure

```text
MiniMica/
├─ src/
│  └─ MiniMicaApp/
│     ├─ .template.config/
│     │  └─ template.json
│     ├─ Configuration/
│     │  └─ AppOptions.cs
│     ├─ Platform/
│     │  └─ Windows/
│     │     ├─ Dwm/
│     │     ├─ Theme/
│     │     ├─ Windowing/
│     │     └─ WindowsVersion.cs
│     ├─ Resources/
│     │  └─ Styles.xaml
│     ├─ Shell/
│     │  ├─ MiniMicaWindow.cs
│     │  └─ WindowAppearanceController.cs
│     ├─ Views/
│     │  └─ MainWindow.xaml(.cs)
│     ├─ App.xaml(.cs)
│     ├─ app.manifest
│     └─ MiniMicaApp.csproj
├─ templates/
│  └─ MiniMica.Templates/
├─ docs/
├─ scripts/
└─ .github/workflows/
```

## `Configuration`

Use this for defaults that should be easy to find and change. Avoid a global mutable state bag. `AppOptions` contains values, not runtime state.

## `Platform/Windows`

The platform root also owns `WindowsVersion.cs`, which queries `RtlGetVersion` directly. Build 18362 (Windows 10 1903) is the minimum to run; `SupportsBackdrop` and `SupportsRoundedCorners` gate the Windows 11 visuals so they degrade instead of blocking startup.

## `Platform/Windows/Dwm`

Owns Desktop Window Manager integration. If Microsoft changes a DWM attribute, this should be the only folder that needs to know.

## `Platform/Windows/Theme`

Owns theme preference/resolution and semantic resources. UI code consumes resource keys instead of testing `if (dark)` repeatedly.

## `Platform/Windows/Windowing`

Owns monitor/DPI/placement operations. This is where window-coordinate conversions belong.

## `Shell`

Contains WPF shell glue reusable across application windows. `MiniMicaWindow` should stay small. If a new feature does not apply to nearly every modern window, do not put it here.

## `Views`

Application UI. This is intentionally the easiest folder to delete or replace.

## `.template.config`

Defines `dotnet new minimica`. The source project remains valid without running the template engine.

## `templates/MiniMica.Templates`

Packages the source template into a NuGet template package. It contains no compiled runtime library.

## Naming rule

The template source project uses `MiniMicaApp`, not `MiniMica`, as the replaceable product identifier. This lets `sourceName` rename downstream project namespaces without rewriting documentation that refers to MiniMica as a project/concept.

### Localization and translation tooling

```text
src/MiniMicaApp/Localization/
├── LocalizationManager.cs
├── LocExtension.cs
├── Strings.cs
└── Strings.resx

tools/localization/
├── generate-resx.ps1
├── worksheet.csv
└── template.resx
```

The application contains only the neutral resource by default. Culture-specific resources are developer-selected/generated so unused languages do not inflate downstream packages.
