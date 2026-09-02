# Troubleshooting

## Application says Windows is unsupported

MiniMica runs on build **18362 or newer** and is designed for build **22621 or newer**. `WindowsVersion` uses `RtlGetVersion` to obtain the actual build number, then gates the Windows 11 visuals through `SupportsBackdrop` and `SupportsRoundedCorners`.

Windows 10 and Windows 11 21H2 are intentionally outside the MiniMica compatibility matrix.

## .NET Framework 4.8 target cannot be built

Install the **.NET Framework 4.8 Developer Pack / targeting pack** through Visual Studio Installer or Microsoft's developer-pack installer. The runtime being present is not the same thing as having reference assemblies needed for compilation.

## The application runs although Windows has .NET Framework 4.8.1

That is expected. .NET Framework 4.x is an in-place runtime family. The project targets 4.8 while newer Windows 11 releases provide the compatible 4.8.1 runtime.

## Mica is not visible

Check:

1. OS build is 18362 or newer to run; 22621 or newer for Mica.
2. High contrast is off.
3. `Backdrop` is not `None`.
4. The root content does not paint an opaque background over the whole client area.
5. `IsBackdropActive` is true.

## Standard WPF controls look too old

MiniMica does not import a Fluent control library because binary size is a design constraint. Use the provided small semantic styles, create application-specific styles, or explicitly add a control library if your product accepts the added dependencies and payload.

## Dark mode does not change immediately

The window listens for Windows setting/theme messages. If you mutate theme-related registry values programmatically, Windows may not broadcast the same notification as Settings. Call `RefreshAppearance()` after an application-controlled change.

## Generated application grew beyond the size target

Run `scripts/test-template.ps1`. It measures the Release shipping payload excluding PDB/XML files. Common causes of growth are:

- runtime `PackageReference`s;
- embedded video/images/fonts;
- WebView2 loaders/runtime choices;
- localization resources;
- copied native DLLs.

The starter itself intentionally has no runtime package dependency.
