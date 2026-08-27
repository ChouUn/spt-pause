# Repository Guidelines

## Project Structure & Module Organization

`Pause.sln` contains one C# project, `source/Pause.csproj`, targeting .NET
Standard 2.1. `source/Plugin.cs` is the BepInEx entry point and registers
Harmony patches. `source/PauseController.cs` owns pause state, timer adjustment,
audio handling, and player state. Patch classes live in
`source/BaseLocalGamePatches.cs` and `source/TickPatches.cs`. Build output belongs
under `source/bin/` and `source/obj/`; both are ignored. The repository currently
has no test or asset directories.

## Build, Test, and Development Commands

Run builds from a Windows command prompt. The project defaults to
`D:\SPT-4.1`; override it with `-p:SptPath=<path>`. Release builds deploy the
plugin unless `DeployToSpt` is disabled.

```bat
dotnet build Pause.sln -c Debug -p:DeployToSpt=false
dotnet build Pause.sln -c Release
```

The first command builds without deployment; the second copies the release DLL
to the configured SPT installation. After building, start SPT, enter an offline
raid, and use `F9` to exercise the plugin.

Create the versioned, install-ready ZIP without deploying it into SPT:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass `
    -File scripts\package-release.ps1
```

Pass `-SptPath "E:\SPT"` to package against another SPT installation. The script
prints the package path and SHA-256 after a successful build.

## Coding Style & Naming Conventions

Follow the existing C# style: four-space indentation, braces on separate lines,
and one class per clear responsibility. Use `PascalCase` for classes, methods,
properties, and patch types; use `camelCase` for parameters and locals and
`_camelCase` for private instance fields. Keep patch classes narrowly focused and
suffix them with `Patch`. Preserve the `Pause` namespace and explicit `using`
directives. No formatter or linter is configured, so match surrounding code.

## Testing Guidelines

There is no automated test suite or coverage threshold. Every change must build
cleanly and be checked in an offline raid. Verify both pause and resume, player
input, AI activity, audio, time of day, and the raid timer's `PAUSED` display.
When adding tests, place them in a separate `tests/` project and name files
`<ClassName>Tests.cs` with behavior-focused test methods.

## Commit & Pull Request Guidelines

History favors short, imperative summaries such as `Update README.md`; release
commits use titles like `v1.4.0 - Updated for 4.0.1`. Keep each commit focused and
mention compatibility changes explicitly. Pull requests should describe the
behavioral change, identify the tested SPT version, list manual verification
steps and results, and include screenshots only for visible UI changes. Do not
commit EFT/SPT assemblies, generated DLLs, or machine-specific credentials.
