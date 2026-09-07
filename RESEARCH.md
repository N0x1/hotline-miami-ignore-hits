# Technical notes

These notes apply to the supported Steam Updated executable, `HotlineGL.exe`.

Game SHA-256:
```
351D096EF0CE221A31416307904A4C10A18487FBE4DA70BF2AC834029614F890
```

## Invincibility

F7 changes the first byte of 13 damage-handler functions from `55` to `C3`, making them return immediately. Turning F7 off restores the original bytes.

The trainer checks the game hash and original code before making changes. It restores memory protection afterwards and attempts to undo changes if a write fails. Addresses and expected bytes are listed in [patch-manifest.json](patch-manifest.json).

## Score boost

F8 raises the kill-score bonus, current score and displayed score to at least 200,000. Higher scores are left alone. The kill-score bonus is included because the results screen recalculates the total.

| Field | Global index |
| --- | --- |
| Kill-score bonus | `0x24A` |
| Current score | `0x69` |
| Displayed score | `0x254` |
| Mission timer | `0x131` |
| Chapter index | `0x271` |

The globals pointer is at module offset `0xBFFCD8`. Each eight-byte entry points to its value at +4. Numeric values use the vtable at module offset `0x6B8BF4`, a type at +4 and a number at +8. Both integers and doubles are supported.

The game is briefly paused while score values are checked and updated, then resumed. Turning F8 off stops further updates without subtracting points.

Version 1.0 checked the wrong timer field (`0x24F`). Version 1.0.1 uses `0x131`, confirmed by the game's timer updates and a live score test. The largest rank threshold found was 138,000; the final A+ result has not been separately verified.

## References

- [Fearless Revolution discussion](https://fearlessrevolution.com/viewtopic.php?t=1320)
- [WeMod engine discussion](https://community.wemod.com/t/hotline-miami-cheats-and-trainer-for-steam/53290)
- [Community source used for comparison](https://github.com/Pi0h1/HotlineMiami.gmx)
- [HLMWadExplorer archive reader](https://github.com/TcT2k/HLMWadExplorer/blob/master/WADArchive.cpp)

No game assets or third-party game source are included.
