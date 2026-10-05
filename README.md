# CannonSoundPoolFix

[中文](README.zh.md) | **English**

A standalone BepInEx 6 plugin for Sprocket. It actively cuts off excess old cannon sounds so that high-rate-of-fire weapons can no longer keep the global audio voice pool saturated, and it caps the unbounded growth of muzzle-effect root instances.

## Features

- Hooks `MuzzleFlashEffect.Setup`; it does not poll vehicles, scenes, or the global `AudioSource`.
- Keeps only the 5 most recent cannon sounds within the same `1 × 1 × 1` world-space region.
- When a 6th nearby cannon sound appears, it stops the near and far `AudioSource` of the oldest entry directly, releasing the audio voice immediately.
- Does not disable `SFX`, `Long range SFX`, `VFX`, `Light`, or the muzzle-effect root node.
- Caps the unbounded muzzle Effect prototype pool at 192 instances; if the game already sets a smaller positive cap, the game's setting is kept.
- Does not modify any game asset files.

## Requirements

- Sprocket `0.2.55.5`
- BepInEx `6.0.0-be.788`, IL2CPP / net6
- Windows x64

Game updates may change the generated IL2CPP types and runtime behavior, so re-verification is required after an update.

## Installation

1. Install BepInEx 6 (IL2CPP) for Sprocket.
2. Drop `CannonSoundPoolFix.dll` into `BepInEx\plugins`.
3. Launch the game; the log should show a load line for `Cannon Sound Pool Fix v2.0.0`.

When sustained fire trips the limit, it logs at most once per scene:

```text
[CSPF] Cannon voice limit engaged: stopped the oldest playing cannon AudioSource pair.
```

## Working with SmokeSuppressor

This mod only manages cannon sound sources and the muzzle Effect lifecycle. It can be installed alongside [SmokeSuppressor](https://github.com/furryaxw/SmokeSuppressor), which continues to hide the smoke output that accumulates over time.

## Building

The default assumes the game is installed at `G:\Sprocket0.2.55.5`:

```powershell
dotnet build .\CannonSoundPoolFix\CannonSoundPoolFix.csproj -c Release -p:SkipModDeploy=true
```

If the game is at a different path:

```powershell
dotnet build .\CannonSoundPoolFix\CannonSoundPoolFix.csproj -c Release `
  -p:SprocketGameRoot="D:\Games\Sprocket" `
  -p:SkipModDeploy=true
```

Omitting `SkipModDeploy` copies the built DLL to `$(SprocketGameRoot)\BepInEx\plugins`.
Do not overwrite a loaded DLL while the game is running.

## License

[GPL-3.0-only](LICENSE.txt)
