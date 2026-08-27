Updated for SPT 4.1.x

This mod will pause the following:
- You
  - Character movement
  - Health
  - Hydration and Energy
  - Stamina
- AI
- Time of day
- Raid Timer will read "PAUSED"

## Developer Notes

The client plugin targets .NET Standard 2.1 and must be built against an SPT
4.1.x installation. The project uses `D:\SPT-4.1` by default. Override that
location with the `SptPath` MSBuild property when SPT is installed elsewhere.

Build without copying the plugin into SPT:

```bat
dotnet build Pause.sln -c Debug -p:DeployToSpt=false
```

Build a release and deploy it to the default SPT installation:

```bat
dotnet build Pause.sln -c Release
```

Build against a different SPT installation and deploy there:

```bat
dotnet build Pause.sln -c Release -p:SptPath="E:\SPT"
```

Combine `-p:SptPath="E:\SPT"` with `-p:DeployToSpt=false` to use another
installation only for assembly references. Build output is written under
`source\bin\<Configuration>\netstandard2.1\`. After code changes, test pause and
resume in an offline raid, including AI, audio, FOV, time of day, and the raid
timer.
