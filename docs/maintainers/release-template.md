# Maintainer guide: release the template

1. Update `Version` in `templates/MiniMica.Templates/MiniMica.Templates.csproj`.
2. Update `CHANGELOG.md`.
3. Run `scripts/test-template.ps1` on Windows. Confirm the source and generated samples remain under the 1.25 MiB shipping-payload budget and have no runtime `PackageReference`.
4. Build a clean package:

   ```powershell
   dotnet pack .\templates\MiniMica.Templates\MiniMica.Templates.csproj -c Release -o .\artifacts
   ```

5. Install the produced `.nupkg` into a clean test environment.
6. Generate at least the default, dark/acrylic, and no-backdrop variants.
7. Build each generated project as .NET Framework 4.8 and inspect its Release payload size.
8. Inspect generated namespaces and filenames for stale `MiniMicaApp` identifiers.
9. Tag the repository only after the package artifact is verified.

The GitHub CI workflow performs the core smoke tests on `windows-latest`.
