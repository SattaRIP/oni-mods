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

- [ ] **Wire-family bridge sprites distort when stretched.** Power Wire and
  Conductive Wire long bridges use round end-terminals + a wavy wire; the uniform
  horizontal stretch (`generate_scaled`) turns the terminals into ovals. Rework to
  keep the end terminals at native size and extend only the middle (element-placement
  approach, like `gen_extended_kanims.py`'s `generate()` for the rail-tile bridge).
