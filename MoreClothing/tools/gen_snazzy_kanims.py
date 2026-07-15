#!/usr/bin/env python3
"""Builds the mod's recoloured kanims from vanilla clothing art.

Extracts the Swimwear (wetsuit_item + worn body_wetsuit) and Rubber Boots
(rubber_boots_item) kanims from the game's Unity assets (UnityPy; run with
~/.venvs/oni-kanim/bin/python), recolours them to the Snazzy Suit's gold
(sampled: HSV 0.145 / 0.755), and renames the builds so the game registers
them as separate kanims. Symbol names inside each build are preserved, so the
worn swimwear override still maps onto the duplicant body.

  swimwear  : red bands -> gold + light sequin sparkle   (item + worn body)
  boots     : yellow rubber -> gold + sequin sparkle      (item only; boots
              have no worn body kanim in vanilla)

Output goes to anim/<name>/<name>/, which build.sh copies into dist/.
Re-run after game updates if Klei changes the clothing art.
"""
import colorsys
import random
import sys
from pathlib import Path

REPO = Path(__file__).resolve().parent.parent
# reuse the roundtrip-validated kanim codec from the bridges mod
sys.path.insert(0, str(REPO.parent / "MagpieExtensionRonivans" / "tools"))
from gen_extended_kanims import parse_build, write_build, parse_anim, write_anim

GAME_DATA = Path.home() / ".local/share/Steam/steamapps/common/OxygenNotIncluded/OxygenNotIncluded_Data"
CACHE = REPO / "tools" / "vanilla_kanim_cache"
ANIM = REPO / "anim"

GOLD_H, GOLD_S = 0.145, 0.755  # sampled from the Snazzy Suit (shirt_decor01) -- swimwear
BOOT_GOLD_H, BOOT_GOLD_S = 0.115, 0.95  # richer, deeper amber-gold for the rubber boots

# vanilla source -> list of (new build name, recolour mode)
JOBS = {
    "wetsuit_item":      [("snazzy_swimwear_item", "red_to_gold")],
    "body_wetsuit":      [("body_snazzy_swimwear", "red_to_gold")],
    "rubber_boots_item": [("snazzy_rubber_boots_item", "yellow_to_gold"),
                          ("snazzy_shoes_item", "tuxedo")],
}


def hsv(r, g, b):
    return colorsys.rgb_to_hsv(r / 255, g / 255, b / 255)


def rgb(h, s, v):
    r, g, b = colorsys.hsv_to_rgb(h, s, v)
    return int(r * 255), int(g * 255), int(b * 255)


def is_red(h, s, v):
    return s > .35 and (h < .08 or h > .92)


def is_yellow(h, s, v):
    return s > .25 and .07 < h < .22


def is_gold(h, s, v):
    return s > .25 and .10 < h < .20


def recolour_image(img, mode, seed):
    from PIL import Image
    out = img.convert("RGBA")
    px = out.load()
    for y in range(out.height):
        for x in range(out.width):
            r, g, b, a = px[x, y]
            if a <= 10:
                continue
            h, s, v = hsv(r, g, b)
            if mode == "tuxedo":
                # rubber boots (yellow) -> glossy black leather dress shoe
                if is_yellow(h, s, v):
                    px[x, y] = rgb(0.0, 0.0, 0.07 + v * 0.13) + (a,)
            elif mode == "red_to_gold":  # swimwear: red bands -> Snazzy-Suit gold
                if is_red(h, s, v):
                    px[x, y] = rgb(GOLD_H, min(0.9, GOLD_S * (0.7 + 0.3 * v)), min(1, v * 1.03)) + (a,)
            else:  # yellow_to_gold: rubber boots -> richer, deeper amber-gold
                if is_yellow(h, s, v):
                    px[x, y] = rgb(BOOT_GOLD_H, min(0.97, BOOT_GOLD_S * (0.75 + 0.25 * v)), min(1, v * 0.97)) + (a,)
    # gold accents
    random.seed(seed)
    if mode == "tuxedo":
        # scatter gold flecks/studs over the black leather for a formal look
        for y in range(out.height):
            for x in range(out.width):
                r, g, b, a = px[x, y]
                if a <= 10:
                    continue
                h, s, v = hsv(r, g, b)
                if v < 0.45 and s < 0.35 and random.random() < 0.07:
                    px[x, y] = (245, 205, 90, a) if random.random() < 0.6 else (190, 150, 40, a)
    else:
        # sequin sparkle on the gold, like the Snazzy Suit
        density = 0.09 if mode == "red_to_gold" else 0.11
        for y in range(out.height):
            for x in range(out.width):
                r, g, b, a = px[x, y]
                if a > 10 and is_gold(*hsv(r, g, b)) and random.random() < density:
                    px[x, y] = (250, 240, 180, a) if random.random() < 0.6 else (120, 96, 20, a)
    return out


def extract():
    import UnityPy
    CACHE.mkdir(parents=True, exist_ok=True)
    wanted_text = set()
    prefixes = tuple(JOBS.keys())
    for src in JOBS:
        wanted_text.add(f"{src}_anim")
        wanted_text.add(f"{src}_build")
    found = {}
    targets = sorted(GAME_DATA.glob("*.assets"))
    targets += sorted((GAME_DATA / "StreamingAssets").glob("*bundle*"))
    for assets in targets:
        env = UnityPy.load(str(assets))
        for obj in env.objects:
            if obj.type.name not in ("TextAsset", "Texture2D"):
                continue
            data = obj.read()
            name = data.m_Name
            if obj.type.name == "TextAsset" and name in wanted_text:
                raw = data.m_Script
                if isinstance(raw, str):
                    raw = raw.encode("utf-8", "surrogateescape")
                (CACHE / f"{name}.bytes").write_bytes(raw)
                found[name] = assets.name
            elif obj.type.name == "Texture2D":
                for p in prefixes:
                    if name.startswith(p + "_") and name[len(p) + 1:].isdigit():
                        data.image.save(CACHE / f"{name}.png")
                        found[name] = assets.name
    print("extracted:", sorted(found))
    missing = {t for t in wanted_text} | {f"{s}_0" for s in JOBS}
    missing -= set(found)
    if missing:
        sys.exit(f"MISSING from game assets: {sorted(missing)}")


def generate():
    from PIL import Image
    for src, outputs in JOBS.items():
        bd = (CACHE / f"{src}_build.bytes").read_bytes()
        ad = (CACHE / f"{src}_anim.bytes").read_bytes()
        # validate the codec roundtrips on this source once
        assert write_build(parse_build(bd)) == bd, f"{src}: build roundtrip failed"
        assert write_anim(parse_anim(ad)) == ad, f"{src}: anim roundtrip failed"

        for new_name, mode in outputs:
            build = parse_build(bd)          # fresh parse per output (we rename it)
            build["name"] = new_name         # symbols preserved, only the build name changes
            out = ANIM / f"{new_name}_anims" / new_name
            out.mkdir(parents=True, exist_ok=True)
            (out / f"{new_name}_build.bytes").write_bytes(write_build(build))
            (out / f"{new_name}_anim.bytes").write_bytes(ad)  # animation data unchanged

            seed = abs(hash(new_name)) % 9999
            for tex in sorted(CACHE.glob(f"{src}_*.png")):
                idx = tex.stem[len(src) + 1:]
                if not idx.isdigit():
                    continue
                recolour_image(Image.open(tex), mode, seed).save(out / f"{new_name}_{idx}.png")
            print("wrote", out)


if __name__ == "__main__":
    if not all((CACHE / f"{s}_build.bytes").exists() for s in JOBS):
        extract()
    generate()
