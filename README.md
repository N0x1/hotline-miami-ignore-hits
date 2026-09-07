# Hotline Miami — Ignore Hits

Experimental F7 invincibility utility for the installed Steam **Updated** version of Hotline Miami (`HotlineGL.exe`).

## Use

1. Run **HotlineMiami-IgnoreHits.exe**.
2. Start Hotline Miami through Steam and choose **Updated** in its launcher.
3. Wait for the utility to show **Updated game connected**.
4. Press **F7** to turn protection **ON**. Press **F7** again to turn it **OFF**. The window button also works.

Enable it before taking a hit. It cannot revive an already dead character.

Keep the utility running while playing. Closing it normally restores the original game code. Protection starts OFF for each new game process. If the utility crashes while protection is on, restarting the game clears it; reopening the utility can also recover a fully patched session.

## Intended protection

- Enemy bullets, including the player’s human-shield and execution states.
- Melee attacks handled by the shared player-death routine.
- The identified dog and panther death routines.

Enemies can still move and attack. Protection bypasses the identified hit/death handlers. F7 restores their original behaviour when switched off.

**Actual combat has not been tested.** Code-level verification passed, including x86 emulation of the bullet routes and real-memory tests of toggling and restoration. Special scripted deaths, hazards, boss transitions and enemy cleanup behaviour remain unverified. Switch protection off if a story event requires it.

For the first gameplay check, use a replayable chapter: test a melee hit with protection ON, then an enemy bullet; turn it OFF and confirm normal damage returns. Dog, panther and special player states need separate gameplay checks.

## Compatibility and troubleshooting

- Supports only the exact installed Updated executable used to develop this build. The utility verifies its SHA-256 and the code at every patch location before writing.
- The Original version (`HotlineMiami_Original.exe`) and Hotline Miami 2 are unsupported.
- If another application already owns F7, the utility says so; use its button.
- If attachment reports conflicting code, restart the game without another trainer.
- If access is denied, run the game and utility at the same privilege level. Administrator rights are not requested automatically.
- `IgnoreHits.log`, next to the utility, records attachment, toggles and errors.

There is no installation into the Steam folder. No game executable, WAD, save, or Steam configuration is modified on disk. The utility changes 13 single-byte function entries in the running game and restores them when disabled. Game saves and achievements otherwise remain under the game’s normal control.

## Verification and source

`verification-offline.json` records the offline dispatch/emulation results. `verification-memory.txt` records the patch engine tests. `patch-manifest.json` lists the exact build and patch locations.

The source package contains `src/` and `build.ps1`. Build with Windows PowerShell using `./build.ps1`; it uses the Windows .NET Framework C# compiler and has no third-party runtime dependency. The development-only `research/` folder is not included in the release package and is not needed to run or build the utility.

Research references: [Fearless Revolution discussion](https://fearlessrevolution.com/viewtopic.php?t=1320), [WeMod author’s explanation of the engine change](https://community.wemod.com/t/hotline-miami-cheats-and-trainer-for-steam/53290), [community-maintained source for comparison](https://github.com/Pi0h1/HotlineMiami.gmx), and [HLMWadExplorer’s archive reader](https://github.com/TcT2k/HLMWadExplorer/blob/master/WADArchive.cpp). Third-party game source and game assets are not included in the release.
