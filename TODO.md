# TODO

## Longer Bridges (MagpieExtension)

- [ ] **Restore automation-overlay tinting for the wide logic bridges (without crashing).**
  The `WideLogicBridgeTinter` `KMonoBehaviour` made the 2-/3-gap Automation Wire
  and Ribbon bridges light up green/red in the automation overlay (signal on/off),
  matching vanilla bridges. Adding it as a per-building component triggered a Mono
  `System.BadImageFormatException: Method has zero rva` dynamic-method JIT crash at
  `KMonoBehaviour.Spawn` when the bridge spawns (same "Mono dynamic-method" crash
  family noted in `MagpieExtension/src/PlacementGuard.cs`). It's been **disabled** so
  the bridges work; the bridges function fully but stay a neutral colour in the
  automation overlay.
  Plan: re-implement the tinting via a **single global manager** (one updater that
  iterates the wide logic bridges and sets their tint) rather than attaching a
  mod-defined `KMonoBehaviour` to each building prefab — avoiding the spawn-path JIT
  failure. Source kept at `MagpieExtension/src/WideLogicBridgeTinter.cs` for reference.

- [ ] **Wire-family bridge sprites distort when stretched (redo widening safely).**
  Power Wire / Conductive Wire long bridges use round end-terminals + a wavy wire;
  uniform `generate_scaled` ovals the terminals. `tools/widen_wire_kanims.py` fixes
  the look (native caps, stretched middle) and the output validates clean — BUT
  shipping it caused an **intermittent launch crash** (silent main-thread death →
  template-precache thread abort; ~2/5 launches). Reverting the electric bases to
  `generate_scaled` was rock-solid (5/5 clean). The trigger was the **custom
  repacked atlas + rewritten build/UVs** (NOT texture size — the Ronivans HP bridges
  are already 512×512 via copied atlases and are fine). So: keep `widen_wire_kanims.py`
  but make it produce art the game's anim loader accepts reliably — likely add symbol
  **bleed/padding** in the repacked atlas, match Klei's atlas packing more closely,
  and/or avoid rewriting UVs (e.g. bake the widened image at the original atlas layout).
  Currently the wire bridges ship with the mild oval-terminal look (scaler).

- [ ] Longer Bridges: longer versions of the heavy-watt joint plates (requested 2026-07-17)
