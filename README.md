# Hotline Miami — Ignore Hits + Score

A standalone Windows trainer for the Steam **Updated** version of Hotline Miami.

**[Download v1.0](https://github.com/N0x1/hotline-miami-ignore-hits/releases/tag/v1.0)**

## Quick start

1. Download and extract `HotlineMiami-IgnoreHits-v1.0.zip` from the release.
2. Close any older version of the trainer, then run `HotlineMiami-IgnoreHits-v1.0.exe`.
3. Launch Hotline Miami through Steam, select **Updated**, and wait for **Updated game connected**.
4. Use the hotkeys below or the buttons in the trainer. Keep it running while you play.

| Hotkey | Action |
| --- | --- |
| **F7** | Toggle invincibility against the identified bullet, melee, dog and panther attacks. |
| **F8** | Toggle a **200,000-point minimum score** boost, intended for A+ mission ranks. |

Press the same key again to switch an option off. Both options work independently.

## Good to know

- Enable F7 before taking a hit; it cannot revive you. Closing the trainer normally restores damage. Restart the game if the trainer crashes.
- Enable F8 during the mission and leave it on through completion. Switching it off **keeps points already added**; the game may save boosted results.
- F7 has been confirmed working in gameplay. **F8 passed automated tests but still needs gameplay confirmation.** Special scripted deaths and boss events are not fully tested.
- Only the verified `HotlineGL.exe` build is supported. The trainer checks compatibility automatically. The Original version and Hotline Miami 2 are unsupported.
- Nothing needs to be installed in the game folder. If a hotkey is unavailable, use its button. Errors are recorded in `IgnoreHits.log` beside the trainer.

## Source and tests

The repository contains development source, tests and research. Ready-to-run downloads are on the Releases page; `dist/` is generated locally when you build.

Build with `./build.ps1` in Windows PowerShell; run automated checks with `./tests/run.ps1`. Uses the Windows .NET Framework compiler.

See [changes](https://github.com/N0x1/hotline-miami-ignore-hits/blob/main/CHANGELOG.md), [technical findings and references](https://github.com/N0x1/hotline-miami-ignore-hits/blob/main/RESEARCH.md), and [research tools](https://github.com/N0x1/hotline-miami-ignore-hits/blob/main/research/README.md) for details.
