# mythraps' Oxygen Not Included mods

Source for my [Oxygen Not Included](https://www.klei.com/games/oxygen-not-included)
mods. Everything here is my own code, MIT-licensed (see [LICENSE](LICENSE)).

## Mods

### Critter Turret
[Steam Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=3756399185)

A red robo-miner that **shoots critters instead of mining rock** — automatic,
hands-off critter population control. Standalone.

- **Population threshold side screen**: only opens fire while the critter count
  in its room is above your target, then stands down.
- **Age targeting**: a button on the turret cycles Adults / Adults & Babies /
  Babies.
- **Mounts on floor, wall, or ceiling** with a directional firing arc (line of
  sight required), like the Robo-Miner.
- Fires the **duplicant multitool's attack laser** (real beam particles and
  sound); kills drop meat and resources normally.
- **Logic port** to disable it from automation. Never targets Duplicants.

Found in the **Shipping** menu next to the Robo-Miner. 120 W, refined metal.

### More Clothing
[Steam Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=3765092134)

Five new garments (merger of the former Protective Wear + Snazzy Swimwear
projects into one mod).

- **Winter Coat** — a Warm Sweater rebuilt into a thicker insulated coat
  (+30 Insulation), at the Clothing Refashionator.
- **Soft Suit** — a lightweight, checkpoint-free Atmo Suit alternative:
  cold/heat/radiation protection, sealed against airborne germs and wetness,
  a big breath reserve dupes actually breathe from, fewer bathroom trips, and
  a helmet that assembles piece by piece when surroundings turn dangerous.
  No air tank — it delays suffocation rather than preventing it.
- **Snazzy Swimwear** (+25 decor) and **Snazzy Rubber Boots** (+15 decor) —
  gold-sequin Refashionator upgrades of the Bionic Booster Pack garments.
- **Shoes** (+15 decor) — black-and-gold dress shoes spun at the Textile Loom.

**Required:** none; standalone. The Soft Suit and Snazzy recipes use Bionic
Booster Pack garments as ingredients; Winter Coat and Shoes are DLC-free.

### Longer Bridges
[Steam Workshop](https://steamcommunity.com/sharedfiles/filedetails/?id=3751316059)

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
game — it fails with "no workshop depot found"), and the Steam build of the
game has **no in-game upload button** (that UI exists only on the WeGame/Rail
platform). Use the free **Oxygen Not Included Uploader** Steam tool (AppID
`636750`): point its content folder at the mod's `dist/` (which must contain
`mod_info.yaml` and a 512×512 `preview.png`), set title/description/tags, and
publish. Updates work the same way via its **Edit** button.
`LongerBridges/publish.sh` is non-functional and kept for reference only.

## Credits

- **Ronivans Legacy – Industrial Revolution** — © its author (optional add-on
  support only; no files bundled).
- **Magpie Bridge (鹊桥)** (Steam Workshop `2861126557`) — the original mod that
  inspired this one and whose building IDs Longer Bridges reuses for seamless
  save migration. © its author, 2022.
- Klei Entertainment — Oxygen Not Included.
