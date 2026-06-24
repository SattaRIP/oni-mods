#!/usr/bin/env python3
"""
Generates wide (4/5-cell) kanim variants for MagpieExtension's vanilla-based
bridges by baking the horizontal stretch into the anim data — replacing the
runtime BridgeHelpers.StretchKanim transform hack.

Sources are the vanilla kanims extracted from the game's Unity assets into
tools/vanilla_kanim_cache/ (see git history / session notes: extracted with
UnityPy from sharedassets0.assets; re-extract after game updates if Klei
changes the art).

Reuses the parser/writer from the MagpieExtensionRonivans generator.
"""
import sys
from pathlib import Path

REPO = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(REPO.parent / "MagpieExtensionRonivans" / "tools"))
import gen_extended_kanims as g

g.OUT_BASE = REPO / "anim" / "magpie_extended_anims"
CACHE = REPO / "tools" / "vanilla_kanim_cache"

def main():
    bases = (
        "utilities_conveyorbridge", "logic_bridge", "logic_ribbon_bridge",
        # standalone plumbing bridges (vanilla art, natively 3-wide). These stretch
        # cleanly. The wire-family bridges (utilityelectricbridge[conductive]) have
        # round end-terminals that distort under uniform scaling, so they are widened
        # separately by widen_wire_kanims.py (keep caps native, extend middle only).
        "utilityliquidbridge", "utilitygasbridge",
        # TEMP: wire-family reverted to scaler for launch stability; widening
        # (widen_wire_kanims.py) caused an intermittent load crash. See TODO.md.
        "utilityelectricbridge", "utilityelectricbridgeconductive",
    )
    for base in bases:
        for w in (4, 5):
            g.generate_scaled(CACHE / base, base, w)

if __name__ == '__main__':
    main()
