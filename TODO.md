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

- [x] Longer Bridges: longer versions of the heavy-watt joint plates (requested
  2026-07-17; DONE 2026-07-18 in v1.1.0 — 2/3-tile Heavi-Watt + Conductive plates,
  insulated 3-tile variants gated on Insulated Joint Plate [FIXED])

## Mod upload / publishing queue

- [ ] **Longer Bridges 1.1.0 → Workshop**: uploader 636750 → select Longer Bridges
  (3751316059) → Edit → Update Data (`LongerBridges/dist`) + Update Details
  (paste from `LongerBridges/WORKSHOP_JOINTPLATES_UPDATE.txt`) → Publish.
- [ ] **Longer Bridges 1.1.0 → Workshop page image**: on the mod's Workshop page
  (browser, owner controls) add `LongerBridges/screenshots_jointplates.png`.
- [ ] **Patreon: Longer Bridges 1.1.0 post** — draft ready at
  `LongerBridges/PATREON_POST_1.1.0.md` (attach `screenshots_jointplates.png`).
- [ ] Patreon: original Longer Bridges + Critter Turret announcement posts
  (drafts: `LongerBridges/PATREON_POST.md`, `CritterTurret/PATREON_POST.md`).
- [ ] Self-sealing Airlocks (U59 Fix): comment the new Workshop link on the
  "Self-sealing Airlocks Revived" page (2542160819) so stranded users find it.
- [ ] More Clothing v1.2.0 → Workshop data update (uploader Edit; change note in
  Guides "Nexus Upload Next Steps.md" Part 3b) — if not already pushed.
- [ ] Nexus: upload prepared ONI zips from `~/Documents/mod-releases/nexus/`
  (rebuild first via `build_nexus_packages.py --build` so 1.1.0 is included).
- [ ] Workshop housekeeping: delete orphan steamcmd item 3750518784.
