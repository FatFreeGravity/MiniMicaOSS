# Recipe: build, package, and preserve the small payload

MiniMica targets .NET Framework 4.8 specifically so ordinary applications can use the framework already present on supported Windows 11 installations.

## Release build

```powershell
msbuild .\src\MiniMicaApp\MiniMicaApp.csproj /restore /t:Build /p:Configuration=Release
```

The distributable files are under:

```text
src\MiniMicaApp\bin\Release\
```

For a dependency-free starter this is essentially the application executable plus small configuration/resource artifacts. Do **not** copy the .NET Framework runtime beside the application.

## Size budget

MiniMica treats size as an architectural test. `scripts/test-template.ps1` sums Release payload files except `.pdb` and `.xml` and fails above **1.25 MiB**.

That budget is intentionally close to the project's ~1 MB goal. Product-specific images, languages, browser runtimes, native libraries, or third-party assemblies will naturally increase the final product size.

## Installer choices

MiniMica does not force MSIX, MSI, an EXE bootstrapper, OEM preload packaging, or another installer technology. Whatever wrapper you choose should package the small `net48` application payload; it should not add a private modern .NET runtime unless your product has separately chosen to leave the MiniMica runtime model.

## Build the template NuGet package

The template packaging project targets `netstandard2.0` only as a convenient build vehicle. That target is **not part of generated applications**.

```powershell
dotnet pack .\templates\MiniMica.Templates\MiniMica.Templates.csproj -c Release -o .\artifacts
```

Install it locally:

```powershell
dotnet new install .\artifacts\MiniMica.Templates.1.0.0.nupkg
```

Then build generated applications with MSBuild/Visual Studio as .NET Framework 4.8 projects.
