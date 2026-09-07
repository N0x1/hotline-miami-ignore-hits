# Offline research tools

These tools read a locally installed copy of the Updated game. Extracted data, disassembly, third-party source and downloaded pages are ignored by Git. They are not required to compile or run the mod.

Use Python 3 with `pip install -r research/requirements.txt`. The current inspection script uses the Steam installation path from this project; adjust `GAME` in `inspect_game.py` for a different installation.

From the repository root, regenerate the offline evidence in this order:

```powershell
python research/inspect_game.py wad
python research/map_objects.py
python research/emulate.py
python research/verify_patch.py
```

`verify_patch.py` checks the executable against the committed patch manifest. `make_manifest.py` regenerates that manifest and `src/Definitions.cs`; use it only after verifying patch addresses for the target build, since it records the hash of whichever executable is installed.

Build and test the native patch engine without opening the game:

```powershell
./build.ps1
Start-Process ./dist/HotlineMiami-IgnoreHits.exe -ArgumentList '--self-test', 'verification-memory.txt' -WindowStyle Hidden -Wait
Get-Content verification-memory.txt
```

The self-test uses memory allocated by its own process. Neither this test nor offline x86 emulation establishes actual gameplay behaviour; see the root README for the pending combat checks.
