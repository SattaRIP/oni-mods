# mythraps' Oxygen Not Included mods

Source for my [Oxygen Not Included](https://www.klei.com/games/oxygen-not-included)
mods. Everything here is my own code, MIT-licensed (see [LICENSE](LICENSE)).

## Mods

### Longer Bridges
A fully **standalone** bridge pack — no other mods required.

- Adds **2-tile and 3-tile gap** bridge variants for **every** utility type:
  **Liquid Pipes**, **Gas Pipes**, **Power Wires**, **Conductive Wires**,
  **Insulated Conductive Wires**, **Automation Wires**, **Automation Ribbons**,
  and **Conveyor Rails**.
- Each appears in its **normal build-menu category**, in **English**.
- If you also run **Ronivans Legacy – Industrial Revolution**, adds gap variants
  for its **Logistic Solid**, **Heavy-Duty**, and **Heavy-Duty Joint Plate**
  bridges (they appear automatically when that mod is present).

**Required:** none. **Optional:** Ronivans Legacy – Industrial Revolution.

Migrating from the old Magpie Bridge (鹊桥) setup is **seamless**: Longer Bridges'
liquid/gas/wire/conductive bridges reuse the same building IDs, so bridges you
already placed survive the switch.

This mod ships only my own DLLs and bundles no third-party files.

### Magpie Bridge Extension / Magpie Bridge Extension — Ronivans Legacy Add-on
The two source projects that Longer Bridges packages together. Kept as separate
build targets; the combined release lives in [`LongerBridges/`](LongerBridges/).

### Pacus Die Out Of Water
Pacus take damage out of water and eventually die. Useful for population control.

## Building

Each mod has a `build.sh` that compiles with Mono `mcs` against the game's
managed assemblies. For the combined release:

```bash
cd LongerBridges
./build.sh        # builds both DLLs and assembles dist/
```

Built `dist/` folders and compiled `*.dll` files are git-ignored; clone and
build to produce them.

## Publishing

ONI mods can't be published with `steamcmd` (it has no workshop depot for the
game — it fails with "no workshop depot found"). Use ONI's **in-game uploader**:
copy `LongerBridges/dist/` to `mods/Dev/LongerBridges/`, launch ONI, then
**Mods → Longer Bridges (Dev) → Upload Mod**. The game writes the
`PublishedFileId` back. `LongerBridges/publish.sh` documents this (the steamcmd
path it contains is non-functional, kept for reference).

## Credits

- **Ronivans Legacy – Industrial Revolution** — © its author (optional add-on
  support only; no files bundled).
- **Magpie Bridge (鹊桥)** (Steam Workshop `2861126557`) — the original mod that
  inspired this one and whose building IDs Longer Bridges reuses for seamless
  save migration. © its author, 2022.
- Klei Entertainment — Oxygen Not Included.
