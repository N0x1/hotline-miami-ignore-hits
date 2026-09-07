# Build and review information

## Release source

[Source for v1.0.1](https://github.com/N0x1/hotline-miami-ignore-hits/tree/v1.0.1) · [Release downloads](https://github.com/N0x1/hotline-miami-ignore-hits/releases/tag/v1.0.1)

The release tag contains the source used for the uploaded trainer.

## Build

On 64-bit Windows with .NET Framework installed, download and extract the release source. Open Windows PowerShell in that folder and run:

```powershell
.\build.ps1
```

The output is `dist\HotlineMiami-IgnoreHits-v1.0.1.exe`. The script uses the Windows .NET Framework C# compiler. It does not download dependencies.

To run the tests:

```powershell
.\tests\run.ps1
```

The tests check memory changes, restoration, score updates and mission detection. They do not open or modify the running game. The separate live score check is recorded in [verification-live-score.txt](verification-live-score.txt).

## What the trainer does

- Connects to the supported `HotlineGL.exe` process and checks its file hash.
- Registers F7 and F8 as hotkeys.
- F7 changes 13 single-byte damage-handler entries in game memory and restores them when disabled or closed normally.
- F8 briefly pauses the game, updates three score values and resumes it.
- Writes a local `IgnoreHits.log`. Test mode also writes its test results.

It makes no network requests, installs no service or startup entry, and does not change game files on disk. The game may save boosted scores through its normal save system.

The EXE is unsigned. Its use of process-memory APIs is part of the trainer's operation; any scanner detections should be assessed during review.

## Download checksums

SHA-256 values for the published files are listed below and in the release checksum file.

`HotlineMiami-IgnoreHits-v1.0.1.exe`
```
4c6495eb5483bedfe37cb09f6353f3d5f3d295609fd1554355636c215967d4d0
```

`HotlineMiami-IgnoreHits-v1.0.1.zip`
```
58d9c290937538d9662d6554b80b6ed4e20f30427158fb5f08018561c92b724f
```
