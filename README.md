# Hotline Miami — Ignore Hits + Score (v0.2)

F7 invincibility and an experimental F8 mission-score boost for the installed Steam **Updated** version of Hotline Miami (`HotlineGL.exe`). The user confirmed the original F7 build works in gameplay.

## Use

1. Close the old utility, then run **HotlineMiami-IgnoreHits-v0.2.exe** from `dist/` or the v0.2 ZIP.
2. Start Hotline Miami through Steam and choose **Updated** in its launcher.
3. Wait for the utility to show **Updated game connected**.
4. Press **F7** to turn protection **ON**. Press **F7** again to turn it **OFF**. The window button also works.
5. Press **F8** to enable the mission-score boost. Press it again to stop boosting. Both options work independently.

F8 keeps the mission's kill-score component, running score and displayed score at least **200,000**. The highest threshold found in this executable is 138,000, so the boosted kill-score component alone exceeds it when the results screen totals the bonuses. Higher existing scores are preserved. Enable F8 during the mission, before the results screen, and leave it on through completion. It stays enabled across missions until switched off or the game exits.

F8 OFF stops future boosts; **it does not remove points already added**. The game can save boosted results and apply its usual score-based rewards. Enabling it after a grade has already been calculated will not retroactively update that grade. F8 waits until a timed mission has initialized and stops with a status message if it encounters an unsupported value layout.

Enable it before taking a hit. It cannot revive an already dead character.

Keep the utility running while playing. Closing it normally restores the original game code. Protection starts OFF for each new game process. If the utility crashes while protection is on, restarting the game clears it; reopening the utility can also recover a fully patched session.

## Intended protection

- Enemy bullets, including the player’s human-shield and execution states.
- Melee attacks handled by the shared player-death routine.
- The identified dog and panther death routines.

Enemies can still move and attack. Protection bypasses the identified hit/death handlers. F7 restores their original behaviour when switched off.

**F7 was confirmed working by the user; F8 still needs gameplay testing.** Code-level verification passed, including x86 emulation of the bullet routes and real-memory tests of toggling and restoration. F8 additionally passed numeric-layout, score-floor, rollback and native cross-process tests. Special scripted deaths, hazards, boss transitions and enemy cleanup behaviour are not exhaustively verified. Switch protection off if a story event requires it.

For the first gameplay check, use a replayable chapter: test a melee hit with protection ON, then an enemy bullet; turn it OFF and confirm normal damage returns. Dog, panther and special player states need separate gameplay checks.

For F8, replay a scored mission, enable the boost, confirm at least 200,000 points appear, finish normally and check for A+. Then switch it off and start a fresh mission to confirm scoring returns to normal.

## Compatibility and troubleshooting

- Supports only the exact installed Updated executable used to develop this build. The utility verifies its SHA-256 and the code at every patch location before writing.
- The Original version (`HotlineMiami_Original.exe`) and Hotline Miami 2 are unsupported.
- If another application already owns F7, the utility says so; use its button.
- The same applies to F8; each hotkey is registered separately.
- If attachment reports conflicting code, restart the game without another trainer.
- If access is denied, run the game and utility at the same privilege level. Administrator rights are not requested automatically.
- `IgnoreHits.log`, next to the utility, records attachment, toggles and errors.

There is no installation into the Steam folder. The utility does not directly edit the game executable, WAD, save files or Steam configuration on disk. F7 changes 13 single-byte function entries in the running game and restores them when disabled. F8 changes the three live score fields; the game may subsequently save the boosted result through its normal scoring system.

## Verification and source

`verification-offline.json` records the offline dispatch/emulation results. `verification-memory.txt` records patch and score tests. `verification-score.json` verifies native score storage and threshold constants. `verification-native-score.txt` records a native score update against an owned 32-bit fixture process. `patch-manifest.json` lists the exact build and F7 patch locations.

The source package contains `src/` and `build.ps1`. Build with Windows PowerShell using `./build.ps1`; it uses the Windows .NET Framework C# compiler and has no third-party runtime dependency. The development-only `research/` folder is not included in the release package and is not needed to run or build the utility.

Run `./test.ps1` to build and run both the patch/score tests and the native 32-bit fixture test. These tests do not launch or attach to Hotline Miami. The original F7-only executable remains in `dist/HotlineMiami-IgnoreHits.exe` as the previously tested baseline.

Research references: [Fearless Revolution discussion](https://fearlessrevolution.com/viewtopic.php?t=1320), [WeMod author’s explanation of the engine change](https://community.wemod.com/t/hotline-miami-cheats-and-trainer-for-steam/53290), [community-maintained source for comparison](https://github.com/Pi0h1/HotlineMiami.gmx), and [HLMWadExplorer’s archive reader](https://github.com/TcT2k/HLMWadExplorer/blob/master/WADArchive.cpp). Third-party game source and game assets are not included in the release.
