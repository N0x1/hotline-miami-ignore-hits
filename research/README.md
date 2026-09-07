# Research tools

Optional tools for checking the installed game. They are not needed to build or run the trainer.

Install Python 3 and the packages in `requirements.txt`. Set the game folder in `inspect_game.py` if your Steam installation is elsewhere.

Run from the repository root:

```powershell
python -m pip install -r research/requirements.txt
python research/inspect_game.py wad
python research/map_objects.py
python research/emulate.py
python research/verify_patch.py
python research/verify_score.py
```

Extracted game data and downloaded reference files stay local and are ignored by Git.

`make_manifest.py` regenerates the patch addresses and game hash. Only use it after checking the addresses for the intended game build.

For normal trainer tests, run `.\tests\run.ps1`. These use test memory and a small test program, not the running game.
