# Bundled Release Workflow

This project now supports a fully bundled Windows release layout:

```text
StS2PortraitModGenerator/
  app/
  tools/
    dotnet/
    godot/
    gdre/
  templates/
  data/
  packages/
  config/
  cache/
  generated/
  artifacts/
  logs/
```

## Prerequisites

Before creating the release bundle, prepare these local inputs:

- a portable `.NET SDK` directory containing `dotnet.exe`
- a MegaDot / Godot 4.5.1 directory with the export executable and its companion files
- populated `packages/` under the repo root
- `gdre/` under the repo root

## Create The Bundle

Run:

```powershell
.\tools\Create-BundledRelease.ps1 `
  -BundledDotnetDir C:\path\to\dotnet `
  -BundledGodotDir C:\path\to\megadot `
  -OutputDir .\dist\StS2PortraitModGenerator
```

The script will:

- publish the GUI and CLI into `app/`
- copy templates, data, packages, and config
- place GDRE under `tools/gdre/`
- place the bundled `.NET SDK` under `tools/dotnet/`
- place the bundled MegaDot / Godot directory under `tools/godot/`
- create the working directories used by the GUI flow

## Runtime Expectations

At runtime, the generator now prefers:

- bundled `dotnet` from `tools/dotnet/dotnet.exe`
- bundled GDRE from `tools/gdre/gdre_tools.exe`
- bundled Godot from `tools/godot/`
- bundled NuGet config from `config/NuGet.config`

The generated mod project is built with:

- fixed package versions
- the bundled NuGet config
- an explicit `GodotPath` MSBuild property

The only dependency that still must come from the end user machine is the installed `Slay the Spire 2` game directory.
