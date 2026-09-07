# Changes

## v0.2

- F8 independently toggles a 200,000-point floor for the mission kill-score bonus and current/displayed score. Existing higher scores are preserved.
- Turning F8 off stops future boosts and keeps points already added. Each new game process starts with F8 off.
- Reads the game's typed numeric values with build-specific validation; briefly pauses the game during each score transaction to avoid racing value updates or room transitions, then resumes it in a finally block.
- Adds score layout, threshold, rollback and native 32-bit fixture verification.
- Keeps F7's 13 patch definitions unchanged. The user reported that F7 worked perfectly before this update.
- F8 gameplay verification is pending.

## Initial baseline (`fb4a2b7`)

- F7 toggles the identified enemy-bullet and player-death handlers.
- Source, testable binary and offline verification saved in the private repository before gameplay testing.
