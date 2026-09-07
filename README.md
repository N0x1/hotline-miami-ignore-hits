# Hotline Miami — Ignore Hits + Score

A standalone trainer for the Steam **Updated** version of Hotline Miami on Windows.

**[Download v1.0.1](https://github.com/N0x1/hotline-miami-ignore-hits/releases/tag/v1.0.1)**

## How to use

1. Download and extract the ZIP, then run `HotlineMiami-IgnoreHits-v1.0.1.exe`. Close any older trainer first.
2. Launch the game through Steam and choose **Updated**.
3. Wait for the trainer to connect. Keep it open while playing.

| Key | Feature |
| --- | --- |
| **F7** | Toggle invincibility. |
| **F8** | Toggle a minimum score of **200,000**, intended for A+ ranks. |

The buttons work too. Press the same key again to turn a feature off.

## Notes

- Turn F7 on before taking damage. It cannot revive you. Some scripted deaths and boss events may still require normal damage.
- F8 can be enabled before or during a mission. It applies when the mission timer starts. Turning it off keeps points already added, and the game may save the result.
- Invincibility and the score boost have been tested in-game. The final A+ grade has not been separately verified.
- Only the supported `HotlineGL.exe` build works. Compatibility is checked automatically. The Original version and Hotline Miami 2 are not supported.
- No installation in the game folder is needed. Closing the trainer normally restores damage; restart the game if the trainer crashes.
- The EXE is unsigned. Errors are saved in `IgnoreHits.log` beside it.

## Source

Build with `.\build.ps1` in Windows PowerShell. Run tests with `.\tests\run.ps1`. Downloads are kept on the Releases page.

[Changes](CHANGELOG.md) · [Build and review information](REVIEW.md) · [Technical notes](RESEARCH.md)
