# mythraps' Oxygen Not Included mods

Source for my [Oxygen Not Included](https://www.klei.com/games/oxygen-not-included)
mods. Everything here is my own code, MIT-licensed (see [LICENSE](LICENSE)).

## Mods

### Magpie Bridges+
An English-language companion and extension pack for the
[Magpie Bridge (鹊桥)](https://steamcommunity.com/sharedfiles/filedetails/?id=2861126557)
mod.

- Adds **2-tile and 3-tile gap** bridge variants for **Automation Wires**,
  **Automation Ribbons**, and **Conveyor Rails**.
- If you also run **Ronivans Legacy – Industrial Revolution**, adds gap variants
  for its **Logistic Solid**, **Heavy-Duty**, and **Heavy-Duty Joint Plate**
  bridges (they appear automatically when that mod is present).
- Renames the base Magpie Bridge's **liquid / gas / wire / conductive-wire**
  bridges to **English** and sorts every bridge into its **correct build-menu
  category** instead of the unlabelled "Label" group.

**Required:** Magpie Bridge (鹊桥). **Optional:** Ronivans Legacy – Industrial Revolution.

This mod ships only my own DLLs. The base liquid/gas/wire bridges come from the
required Magpie Bridge mod; PLib is provided by it at runtime.

### Magpie Bridge Extension / Magpie Bridge Extension — Ronivans Legacy Add-on
The two source projects that Magpie Bridges+ packages together. Kept as separate
build targets; the combined release lives in [`MagpieBridgesPlus/`](MagpieBridgesPlus/).

### Pacus Die Out Of Water
Pacus take damage out of water and eventually die. Useful for population control.

## Building

Each mod has a `build.sh` that compiles with Mono `mcs` against the game's
managed assemblies. For the combined release:

```bash
cd MagpieBridgesPlus
./build.sh        # builds all three DLLs and assembles dist/
```

Built `dist/` folders and compiled `*.dll` files are git-ignored; clone and
build to produce them.

## Publishing

`MagpieBridgesPlus/publish.sh` uploads the contents of `dist/` to the Steam
Workshop via `steamcmd` (you must be logged into the Steam account that owns
ONI; it prompts for credentials + Steam Guard). After the first publish, set the
returned `publishedfileid` in `workshop_item.vdf` so later runs update the same
item, and add **Magpie Bridge (鹊桥)** as a Required Item on the Workshop page.

## Credits

- **Magpie Bridge (鹊桥)** (Steam Workshop `2861126557`) — the base mod these
  extend. © its author, 2022.
- **Ronivans Legacy – Industrial Revolution** — © its author.
- **[PLib](https://github.com/peterhaneve/ONIMods)** by Peter Han — MIT licensed.
- Klei Entertainment — Oxygen Not Included.
