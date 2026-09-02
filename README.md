# MiniMica

A small, fork-friendly WPF starter for desktop apps that look like Windows 11 and
ship in about **1 MB**.

MiniMica gives you the plumbing you would otherwise rebuild every time: a custom
title bar, Mica backdrop, light/dark/contrast theming, DPI handling, an OEM
settings dialog, and a 25-language localization pipeline. All of it is small
enough to read in an afternoon and change without fear.

**v5.0** · .NET Framework 4.8 · Windows 10 1903 or later · MIT

---

## Why it exists

Simple OEM-distributed apps have an awkward set of requirements: they must look
current, honor light/dark and accessibility themes, ship in many languages, pass
compliance review for notification and telemetry opt-out, and stay tiny, because
they are preinstalled on millions of machines.

WinUI solves the look and costs ~20 MB. Plain WPF is small but looks dated and
leaves you to build the chrome. MiniMica is the middle: a WPF app that looks like
a Windows 11 app, with **no runtime NuGet dependencies** and no private .NET
runtime to ship.

| | MiniMica | WinUI 3 |
|---|---|---|
| Typical payload | **~1 MB** | ~20 MB |
| Runtime | already on Windows | ships or requires WindowsAppSDK |
| Look | Windows 11 chrome, Mica | Windows 11 native |
| Model | fork and own it | reference and track |

---

## What you get

| | |
|---|---|
| **Custom title bar** | icon, title, Settings gear and caption buttons, pixel-matched to Windows 11 |
| **Snap Layouts** | the Windows 11 flyout works over the custom maximize button |
| **Fixed design canvas** | lay out at a real size; centered when wide, pan buttons when narrow, never scaled |
| **Three themes** | light, dark and Windows contrast themes, switching live with no code |
| **Mica backdrop** | via DWM, with acrylic/tabbed/none as options |
| **OEM settings** | appearance, notification and telemetry opt-out, persisted to `app.config` |
| **Localization** | spreadsheet → `.resx` → satellites, 25 cultures included |
| **Rename** | `dotnet new minimica -n Contoso` leaves no trace of the original name |
| **Offline verification** | static checks that run on any OS before you compile |

---

## Quick start

```powershell
# Requires Visual Studio 2022/2026 with the .NET Framework 4.8 targeting pack
git clone https://github.com/FatFreeGravity/MiniMicaOSS.git
cd MiniMicaOSS
msbuild .\MiniMica.slnx /restore /p:Configuration=Release
```

Or generate your own app from the template:

```powershell
dotnet pack .\templates\MiniMica.Templates\MiniMica.Templates.csproj -c Release -o artifacts
dotnet new install .\artifacts\MiniMica.Templates.5.0.0.nupkg
dotnet new minimica -n Contoso
```

Then read **[docs/branding.md](docs/branding.md)** and make it yours.

---

## Documentation

**Start here**

| Doc | For |
|---|---|
| [concepts.md](docs/concepts.md) | the five ideas behind the design; read this first |
| [branding.md](docs/branding.md) | step-by-step: make it your product |
| [localization.md](docs/localization.md) | step-by-step: the spreadsheet pipeline |
| [getting-started.md](docs/getting-started.md) | build, run, generate |

**Reference**

| Doc | For |
|---|---|
| [architecture.md](docs/architecture.md) | how the shell fits together |
| [project-structure.md](docs/project-structure.md) | what each folder is |
| [customization.md](docs/customization.md) | extension points |
| [v5-test-protocol.md](docs/v5-test-protocol.md) | the manual Windows test matrix |
| [troubleshooting.md](docs/troubleshooting.md) | known traps |
| [decisions/](docs/decisions/) | why the big choices were made |

---

## Verify before you build

```bash
python3 tools/verify/verify.py
```

Runs on any OS in a second and checks what a compiler will not: missing resource
keys, XAML wired to methods that do not exist, theme keys absent from a theme
branch, brush roles that vanish under contrast themes, template-rename drift, and
the pixel contracts inherited from v4.1.

Every rule was added because that exact defect shipped once. CI gates the Windows
build on it. See [tools/verify/README.md](tools/verify/README.md).

---

## Constraints worth knowing

- **.NET Framework 4.8** and C# 7.3, chosen for payload size rather than nostalgia
- **Windows 10 1903+** runs; Windows 11 22H2+ for the full look. Mica, rounded corners, Snap Layouts and Segoe Fluent Icons degrade on Windows 10 rather than blocking startup
- **No runtime NuGet dependencies** in the base template
- **No RTL support**, because the fixed canvas assumes left-to-right
- Payload budget **1.25 MiB**, enforced by CI

---

## Contributing

See [CONTRIBUTING.md](CONTRIBUTING.md). Two house rules:

1. `python3 tools/verify/verify.py` must pass.
2. If you fix a class of bug, add a rule that catches it, and prove the rule fires
   by breaking the tree on purpose first.

---

MIT licensed. Copyright © 2025-2026 Fat Free Gravity. See [LICENSE](LICENSE).
