# Embed satellite resource DLLs into the main EXE

Use this only when a downstream product values a single-file deployment more than standard .NET resource probing.

The normal MiniMica recommendation is to ship culture folders such as:

```text
MyApp.exe
fr-FR\MyApp.resources.dll
ja-JP\MyApp.resources.dll
zh-CN\MyApp.resources.dll
```

This is simple, debuggable, and supported directly by `ResourceManager`.

## Why embedding is optional

Embedding satellite DLLs does not make their bytes disappear; it mainly reduces file count. It also requires an `AssemblyResolve` hook and an explicit build/copy process, so it is not appropriate for the tiny default template.

## Resolver pattern

If you embed renamed satellite DLLs as resources, register the resolver before localized resources are first requested:

```csharp
public App()
{
    AppDomain.CurrentDomain.AssemblyResolve += ResolveEmbeddedSatellite;
}
```

A resolver can inspect `new AssemblyName(args.Name).CultureName`, find an embedded resource ending in `.<culture>.resources.dll`, read it into a byte array, and return `Assembly.Load(bytes)`.

Do not hard-code the original `MiniMica` namespace in the manifest resource name. A template-generated application has a different assembly/root namespace.

## Build implication

A normal build will still produce culture folders before you embed their contents. If your packaging process requires a true single EXE, add an explicit packaging/post-build step that embeds the satellite assemblies and then removes the external copies from the final staging directory. Keep this packaging transformation outside MiniMica's runtime core.
