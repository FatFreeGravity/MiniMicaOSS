# Getting started

**Runs on:** .NET Framework 4.8 · Windows 10 1903 (build 18362) or newer.
**Designed for:** Windows 11 22H2 (build 22621) or newer.

On Windows 10 the app runs and every function works; what you lose is Mica, rounded
corners, the Snap Layouts flyout and the Segoe Fluent Icons drawings. Verified on
Windows 10 20H1 (build 19041).

---

## 1. Prerequisites

- **Visual Studio 2022 or 2026** with the *.NET desktop development* workload
  and the **.NET Framework 4.8 targeting pack**
- **Python 3** (optional) for the verification suite and the cross-platform
  localization generator
- A **.NET SDK**, needed only to pack or install the `dotnet new` template.
  It is a build-time tool, never a runtime dependency of the generated app.

The solution is `MiniMica.slnx`, the XML solution format. Visual Studio 2026
opens it directly; on older Visual Studio, open the `.csproj` files instead.

---

## 2. Verify, then build

```bash
python3 tools/verify/verify.py
```

One second, any OS, and it catches classes of defect a compiler will not. Worth
running before every build.

```powershell
# Developer PowerShell for Visual Studio
msbuild .\MiniMica.slnx /restore /p:Configuration=Release
```

Or use the triage wrapper, which also reports the payload against the budget:

```powershell
.\scripts\build-and-report.ps1
```

Run `src\MiniMicaApp\bin\Release\MiniMicaApp.exe`. You should see the Reykjavik
sample page: a 960 × 640 canvas under a 30 DIP custom title bar.

---

## 3. Try the shell

| Action | What it demonstrates |
|---|---|
| Hover the maximize button | Windows 11 Snap Layouts over custom chrome |
| Drag the window narrower than 960 | canvas clips, pan chevrons appear |
| Drag it wider | canvas centers with padding, never scales |
| Click the gear | OEM settings dialog |
| Switch Appearance to Dark | palette and bullet icons change, no restart |
| Windows Settings → Accessibility → Contrast themes | caption band fills, 4 DIP outline appears |
| Open Settings holding **Ctrl+Shift** | hidden developer language picker |

---

## 4. Generate your own app

```powershell
dotnet pack .\templates\MiniMica.Templates\MiniMica.Templates.csproj -c Release -o artifacts
dotnet new install .\artifacts\MiniMica.Templates.5.0.0.nupkg
dotnet new minimica -n Contoso
```

Options:

```powershell
dotnet new minimica -n Contoso --theme Dark --backdrop Acrylic
```

| Parameter | Values | Default |
|---|---|---|
| `--theme` | `System` `Light` `Dark` | `System` |
| `--backdrop` | `Mica` `Acrylic` `Tabbed` `Auto` `None` | `Mica` |

Renaming covers namespaces, assembly name, `x:Class`, the `ResourceManager` base
name, the manifest identity and the settings path. Confirm with:

```bash
python3 tools/verify/verify.py rename
```

To uninstall the template: `dotnet new uninstall MiniMica.Templates`.

---

## 5. Next

| Goal | Read |
|---|---|
| Understand the design | [concepts.md](concepts.md) |
| Make it your product | [branding.md](branding.md) |
| Ship it in other languages | [localization.md](localization.md) |
| Coming from v4.1 | [migrating-from-v41.md](migrating-from-v41.md) |
| Test it properly | [v5-test-protocol.md](v5-test-protocol.md) |

---

## Troubleshooting the first build

| Symptom | Cause |
|---|---|
| `MSB3644` reference assemblies not found | .NET Framework 4.8 targeting pack missing |
| `msbuild` not recognized | use *Developer PowerShell for Visual Studio* |
| Cannot open `.slnx` | older Visual Studio; open the `.csproj` files directly |
| App exits with a version message | Windows build below 18362 |
| Window renders but flat gray | DWM declined the backdrop, which is expected in a VM or over RDP |

More in [troubleshooting.md](troubleshooting.md).
