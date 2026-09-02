# Forking without the command line

You do not need `dotnet` to start a new app from MiniMica. Two routes, both
entirely inside Visual Studio.

Renaming works under either one because nothing depends on a hardcoded product
name any more: `Strings` derives its resource base name from its own namespace,
and `AppOptions.DisplayName` reads the assembly's `Product` attribute.

---

## Route A: Export Template (recommended)

Visual Studio can turn the project into a reusable template. Do this once, then
create as many apps as you like from the New Project dialog.

**Make the template**

1. Open `MiniMica.slnx`.
2. Select the `MiniMicaApp` project in Solution Explorer.
3. **Project → Export Template…**
4. Choose **Project template**, click Next.
5. Set a template name (`MiniMica App`), a description, and an icon if you want one.
6. Leave **Automatically import the template into Visual Studio** checked.
7. Finish.

**Use it**

1. **File → New → Project**
2. Search for the template name.
3. Enter your project name, for example `Contoso`, and create.

Visual Studio replaces the namespaces and the assembly name with your project
name as it copies the files.

**Two manual touches afterwards**

| File | Change | Why |
|---|---|---|
| `app.manifest` | `name="MiniMicaApp.app"` → `name="Contoso.app"` | cosmetic; Export Template does not rewrite manifest attributes |
| `Properties/AssemblyInfo.cs` | set `AssemblyTitle`, `AssemblyProduct`, `AssemblyCompany`, `AssemblyCopyright` | Export Template rewrites namespaces, not attribute strings. `AppOptions.DisplayName` reads `AssemblyProduct` |

Then build and run. The title bar should read `Contoso`.

---

## Route B: Copy and rename

Fine for a one-off, and it shows exactly what a rename touches.

1. Copy `src/MiniMicaApp` to `src/Contoso`.
2. Rename `MiniMicaApp.csproj` to `Contoso.csproj`.
3. Open it in Visual Studio and edit these two properties:

   ```xml
   <RootNamespace>Contoso</RootNamespace>
   <AssemblyName>Contoso</AssemblyName>
   ```

   Then edit `Properties/AssemblyInfo.cs`, which owns identity and version:

   ```csharp
   [assembly: AssemblyTitle("Contoso")]
   [assembly: AssemblyProduct("Contoso")]
   [assembly: AssemblyCompany("Your Company")]
   [assembly: AssemblyCopyright("Copyright © 2026 Your Company")]
   ```

4. **Edit → Find and Replace → Replace in Files** (`Ctrl+Shift+H`),
   scope *Entire Solution*: `MiniMicaApp` → `Contoso`.
   This covers namespaces, `x:Class` attributes, and the `using` directives.
5. Update `app.manifest`: `name="Contoso.app"`.
6. Delete the `.template.config` folder, which only matters to `dotnet new`.
7. Build.

---

## Route C: the CLI, for automation

Still there, and it is the one covered by the verification suite:

```powershell
dotnet pack .\templates\MiniMica.Templates\MiniMica.Templates.csproj -c Release -o artifacts
dotnet new install .\artifacts\MiniMica.Templates.5.0.0.nupkg
dotnet new minimica -n Contoso --theme Dark --backdrop Mica
```

Use this in scripts and CI. It also handles the `--theme` and `--backdrop`
switches, which the Visual Studio routes do not: set those in `AppOptions`
by hand instead.

---

## What a rename has to cover

Useful to know whichever route you take:

| Item | Handled by |
|---|---|
| Namespaces, `using`, `x:Class` | Export Template, or Replace in Files |
| Assembly name, root namespace | the four `.csproj` properties |
| `ResourceManager` base name | automatic (derived from the namespace) |
| Product name in the UI | reads `AssemblyProduct` from AssemblyInfo.cs |
| Settings folder under `%LOCALAPPDATA%` | automatic (uses the assembly name) |
| `app.manifest` identity | manual, cosmetic |
| `.template.config` | delete it unless you are maintaining a template |

After any route:

```bash
python3 tools/verify/verify.py
```

The `rename` check simulates the CLI route specifically, so it will not validate
an Export Template result. Building and running is the check that matters there.
