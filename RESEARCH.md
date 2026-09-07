# Build-specific findings

Target: `C:\Program Files (x86)\Steam\steamapps\common\hotline_miami\HotlineGL.exe`.

SHA-256: `351D096EF0CE221A31416307904A4C10A18487FBE4DA70BF2AC834029614F890`.

The executable is 32-bit native x86, with preferred image base `0x400000`.

The archive is the first game's WAD variant: a 32-bit data offset, file count, then entries containing a length-prefixed filename, 32-bit size, and 32-bit relative offset. Its collision-event data is 854 object entries with 160 total collision routes. Sprite paths and parent relationships permit object classification even though readable GML names are absent from the executable's gameplay logic.

Key object IDs: 44 is the player parent; 0 is a normal player state; 160 uses the human-shield sprite; 589 is the Biker player parent for 656. Enemy bullet ID is 25; player bullet ID is 8. Object 26 uses the dead-player sprite. These are object IDs, not health addresses.

The engine's collision dispatcher at preferred VA `0x41EF00` resolves callbacks through `0x7074D0`. The latter selects event tables and object-specific selector functions. Executing that selector in Unicorn maps all 160 routes without launching the game. Thirty-two routes from player descendants target enemy bullet 25; they resolve to ten unique callback functions. One callback (`0x659970`) is already empty and is left untouched. The nine other callbacks, plus four shared player-death functions, form the patch manifest.

Shared death entry points (preferred VAs): `0x744B40` for melee, `0x749000` and `0x74D2F0` for the two dog branches, `0x74B270` for panthers. Melee has 15 direct callers in the gameplay code; dog routines are called from the two dog step variants; panther calls occur in its attack code. These are cdecl routines with caller-cleaned arguments; inspected call sites do not consume a return value. Their behaviour and player iteration correspond to the player-death source routines used for comparison. The Original-version cheat table's string patches cannot be applied directly to this executable.

Each selected entry starts with `push ebp` (`55`). Enabling replaces that byte with `ret` (`C3`), before prologue, state mutation or death animation. A single-byte entry patch avoids writing a multi-byte detour while game threads are running. Disabling restores `55`. A call already executing inside a damage handler can still complete, so protection should be enabled before combat. No remote allocation, injected DLL, executable-on-disk patch, pointer freeze, AI freeze or save edit is used.

The utility computes the on-disk executable's SHA-256, obtains the actual process module base for ASLR, checks every five-byte entry signature, applies each byte under temporary writable protection, restores the previous page protection, flushes the instruction cache and verifies the result. Failed writes trigger rollback. Full prior patch state can be recovered after a utility crash; mixed or foreign code is refused.

Offline checks establish dispatch coverage, immediate return with no writes outside the emulated stack, unchanged unrelated collision callbacks, and exact restoration. Native fixture tests establish repeated toggling, page-protection restoration, failed-transaction rollback, conflict rejection and prior-session recovery. They do not establish behaviour in actual combat, story sequences, bosses or game progression. In particular, caller-side cleanup after a skipped death routine may still run.

A boolean-looking state does not establish its storage width, and bypassing a state transition requires identifying all associated side effects. This build patches the identified damaging paths directly.

## F8 score boost (v1.0)

The native globals accessor at `0x407890` indexes eight-byte wrappers using the pointer at preferred VA `0xFFFCD8`. Each wrapper's second dword points to a typed value object. Its expected vtable is `0xAB8BF4`, type is at +4, and payload is at +8. Getter `0x8D4E90` dispatches type 1 to an int32 read and type 2 to a double read. The utility supports both without changing types, ownership pointers or vtables.

The score reset routine `0x805BC0` identifies globals `drawscore=0x254`, `myscore=0x69`, `killscore=0x24A` and `time=0x24F`. Initialization `0x805DC0` identifies `currentlevel=0x271`. Results initializer `0x5C0BD0` resets `myscore`, copies `killscore` to bonus[0], then totals the bonus array into `myscore`. Therefore a display-only edit would be lost; F8 raises the kill-score component as well.

The native max-points routine at `0x8131F0` contains 16 mission-specific values plus a 40,000 fallback; its largest value is 138,000. F8's 200,000 floor exceeds every extracted value. Rank calculation consumes `myscore` in the score-details initializer `0x6A7780`. The public source comparison uses strictly greater-than max points for A+.

F8 is a separate timer-driven data operation. It briefly suspends the target process, resolves current pointers, validates mission time, level, numeric types and plausible values, raises only values below the floor, verifies writes, and resumes the process in `finally`. Failed writes attempt rollback while still paused. Disabling F8 does not subtract points, and no high-score or achievement arrays are written directly. The game may persist the resulting score normally. Initial F7 gameplay was confirmed by the user; F8 still needs an actual mission/result-screen check.

## References

- [Fearless Revolution discussion](https://fearlessrevolution.com/viewtopic.php?t=1320)
- [WeMod author's explanation of the engine change](https://community.wemod.com/t/hotline-miami-cheats-and-trainer-for-steam/53290)
- [Community-maintained source used for comparison](https://github.com/Pi0h1/HotlineMiami.gmx)
- [HLMWadExplorer archive reader](https://github.com/TcT2k/HLMWadExplorer/blob/master/WADArchive.cpp)

Third-party game source and game assets are not included in this repository or its releases.
