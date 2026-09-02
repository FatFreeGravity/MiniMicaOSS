using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;

// Assembly identity, version and copyright live here rather than in the .csproj.
//
// An SDK-style project generates these attributes from MSBuild properties by default,
// which is why <GenerateAssemblyInfo>false</GenerateAssemblyInfo> is set in
// MiniMicaApp.csproj. Without that opt-out, having both produces CS0579 duplicate
// attribute errors. With it, this file is the single source of truth.
//
// A fork edits Title, Company, Product and Copyright here. `dotnet new` rewrites the
// "MiniMicaApp" strings for you; Visual Studio's Export Template does not, so change
// them by hand on that route.

[assembly: AssemblyTitle("MiniMicaApp")]
[assembly: AssemblyDescription("A tiny Windows 11 WPF application starter with custom chrome, fixed-page layout and OEM settings.")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Fat Free Gravity")]
[assembly: AssemblyProduct("MiniMicaApp")]
[assembly: AssemblyCopyright("Copyright © 2025-2026 Fat Free Gravity")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Types in this assembly are not exposed to COM.
[assembly: ComVisible(false)]

// WPF theme resource lookup. None: no theme-specific dictionaries; SourceAssembly:
// the generic dictionary ships in this assembly.
[assembly: ThemeInfo(
    ResourceDictionaryLocation.None,
    ResourceDictionaryLocation.SourceAssembly)]

// Version is Major.Minor.Build.Revision, where Build is the date-derived number.
// The Settings dialog shows Major.Minor.Build via AppVersion.Display, which is what
// the {M.m.build} placeholder in the localized "settings_version" string expects.
[assembly: AssemblyVersion("5.0.26242.0")]
[assembly: AssemblyFileVersion("5.0.26242.0")]
