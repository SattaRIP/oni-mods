#!/usr/bin/env python3
"""Builds the Protective Wear mod's recoloured kanims from vanilla clothing art.

Run with ~/.venvs/oni-kanim/bin/python (needs UnityPy + Pillow).

  EVA Suit          <- Swimwear (wetsuit_item + worn body_wetsuit)
                       black suit -> white/silver spacesuit fabric,
                       red bands  -> orange EVA accents.
                       The wetsuit hood reads as a small EVA head covering
                       (deliberately smaller than the Atmo Suit helmet).
  Upgraded Warm Coat <- Warm Sweater (shirt_hot_shearling item + worn body)
                       tan shearling -> deep insulated blue, cream fluff kept
                       as trim, so it is clearly a heavier upgraded coat.

Symbol names inside each build are preserved (only build["name"] changes), so
the worn overrides still map onto the duplicant body. Output goes to
anim/<name>_anims/<name>/, which build.sh copies into dist/.
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

# vanilla source build -> (new build name, recolour mode)
JOBS = {
    "wetsuit_item":             ("eva_suit_item", "eva"),
    "body_wetsuit":             ("body_eva_suit", "eva"),
    "shirt_hot_shearling":      ("upgraded_warm_coat_item", "warm"),
    "body_shirt_hot_shearling": ("body_upgraded_warm_coat", "warm"),
    # Refashionator station art with the yellow snazzy-suit garment turned
    # rainbow; used as a symbol-override source (its "object" symbol) while the
    # station works on one of our recipes. Only the garment pixels matter --
    # the rest of the atlas (spools, bottles...) is never drawn from this copy.
    "super_snazzy_suit_alteration_station": ("refashion_rainbow", "rainbow"),
    # Hidden companion boots the Soft Suit auto-equips into the SHOES slot;
    # vanilla yellow rubber shifted to the suit's orange accent colour. The
    # snapTo_foot symbol rides along, so worn feet show orange boots too.
    "rubber_boots_item":        ("eva_boots_item", "boots"),
}

# MCU-style helmet assembly: pivot-shifted copies of the game's build-only
# headgear builds (art unchanged, nudged relative to the head anchor), stepped
# through at runtime so each part appears to slide into place. dy is a
# fraction of the sprite height added to every frame's pivotY; positive is
# intended as "art displaced away from its seated position" (dome starts high,
# mask starts low). If a slide moves the wrong way in game, flip SLIDE_SIGN.
# These sources have no *_anim file (build-only), and neither do our copies.
SLIDE_SIGN = 1.0
SLIDES = {
    "atmo_helmet_clear": [("eva_dome_s1", 0.9), ("eva_dome_s2", 0.45)],
    "mask_oxygen":       [("eva_mask_s1", -0.5)],
}


def hsv(r, g, b):
    return colorsys.rgb_to_hsv(r / 255, g / 255, b / 255)


def rgb(h, s, v):
    r, g, b = colorsys.hsv_to_rgb(h, s, v)
    return int(round(r * 255)), int(round(g * 255)), int(round(b * 255))


def is_red(h, s, v):
    return s > .35 and (h < .08 or h > .92)


def recolour_eva(img, seed):
    """Black wetsuit -> yellow suit fabric; red bands -> orange accents.

    Continuous per-pixel blending -- NO hard thresholds. The first version
    used category cutoffs (is_red / s<0.28), so anti-aliased edge pixels fell
    on either side of them and the suit came out speckled and blocky in game.
    Here every pixel gets a smooth mix weighted by how red / how grey it is,
    and the fabric brightening is a gamma curve (v**0.45) so dark outlines
    stay dark instead of washing out flat.
    """
    def clamp01(x):
        return 0.0 if x < 0 else (1.0 if x > 1 else x)

    out = img.convert("RGBA")
    px = out.load()
    for y in range(out.height):
        for x in range(out.width):
            r, g, b, a = px[x, y]
            if a <= 10:
                continue
            h, s, v = hsv(r, g, b)

            # how red this pixel is (hue distance to 0, needs some saturation)
            dh = min(h, 1.0 - h)
            red_w = clamp01(1.0 - dh / 0.14) * clamp01(s / 0.30)
            # how grey/unsaturated it is (full weight below s=0.15 so the
            # near-black fabric recolours completely, fading out by s=0.30).
            # The wetsuit fabric sits at v ~0.03-0.15 with the OUTLINES at
            # v ~0.0, so lift the fabric with a value floor but keep a soft
            # guard that leaves true black line art black.
            dark_keep = clamp01((0.05 - v) / 0.03)
            grey_w = clamp01((0.30 - s) / 0.15) * (1.0 - red_w) * (1.0 - dark_keep)

            sr, sg, sb = rgb(0.135, 0.85, 0.55 + (v ** 0.6) * 0.42)  # yellow fabric
            orr, org, orb = rgb(0.055, max(0.65, min(0.95, s)),
                                min(1.0, v * 1.05))            # orange accent

            nr = r * (1 - grey_w - red_w) + sr * grey_w + orr * red_w
            ng = g * (1 - grey_w - red_w) + sg * grey_w + org * red_w
            nb = b * (1 - grey_w - red_w) + sb * grey_w + orb * red_w
            px[x, y] = (int(round(nr)), int(round(ng)), int(round(nb)), a)
    return out


def recolour_warm(img, seed):
    """Tan shearling -> deep insulated blue; cream fluff kept as trim."""
    out = img.convert("RGBA")
    px = out.load()
    for y in range(out.height):
        for x in range(out.width):
            r, g, b, a = px[x, y]
            if a <= 10:
                continue
            h, s, v = hsv(r, g, b)
            # warm tan/brown body (skip the near-white cream fluff, s<0.2)
            if s > 0.20 and 0.015 < h < 0.16:
                nr, ng, nb = rgb(0.57, min(0.85, s * 1.05), v)
                px[x, y] = (nr, ng, nb, a)
    return out


def is_garment_yellow(h, s, v):
    """The snazzy suit's yellow (skip dark trim and unsaturated pixels)."""
    return s > 0.30 and 0.06 < h < 0.22 and v > 0.25


def recolour_rainbow(img, build):
    """Turn the station's "object" symbol (the yellow garment) rainbow.

    Only the object symbol's atlas frames are touched, and the gradient is
    anchored to each frame's own bounding box (not the atlas), so every
    animation frame carries the same rainbow bands instead of picking up
    whatever colour happens to cross its atlas position. Klei build UVs may be
    measured from either the top or the bottom of the texture; whichever
    orientation actually contains the yellow garment is the right one.
    """
    out = img.convert("RGBA")
    px = out.load()
    W, H = out.size

    table = dict(build["hashes"])
    rects = []  # (x0, x1, top-down y0/y1, bottom-up y0/y1)
    for s in build["symbols"]:
        if table.get(s["hash"]) != "object":
            continue
        for f in s["frames"]:
            u0, v0, u1, v1 = f[-4:]
            rects.append((int(u0 * W), int(u1 * W),
                          int(v0 * H), int(v1 * H),
                          int((1 - v1) * H), int((1 - v0) * H)))

    def yellow_frac(x0, x1, y0, y1):
        total = yellow = 0
        for y in range(max(y0, 0), min(y1, H)):
            for x in range(max(x0, 0), min(x1, W)):
                r, g, b, a = px[x, y]
                if a <= 10:
                    continue
                total += 1
                if is_garment_yellow(*hsv(r, g, b)):
                    yellow += 1
        return yellow / max(total, 1)

    flipped = (sum(yellow_frac(x0, x1, fy0, fy1) for x0, x1, _, _, fy0, fy1 in rects) >
               sum(yellow_frac(x0, x1, y0, y1) for x0, x1, y0, y1, _, _ in rects))

    for x0, x1, ty0, ty1, fy0, fy1 in rects:
        y0, y1 = (fy0, fy1) if flipped else (ty0, ty1)
        w, h = max(x1 - x0, 1), max(y1 - y0, 1)
        for y in range(max(y0, 0), min(y1, H)):
            for x in range(max(x0, 0), min(x1, W)):
                r, g, b, a = px[x, y]
                if a <= 10:
                    continue
                hh, ss, vv = hsv(r, g, b)
                nh = (((x - x0) / w + (y - y0) / h) * 0.75) % 1.0
                if is_garment_yellow(hh, ss, vv):
                    nr, ng, nb = rgb(nh, min(0.9, ss), vv)
                elif ss < 0.35 and vv < 0.30:
                    # Black fabric -> the same rainbow, kept dark. Two of the
                    # four frames are PURE black (fabric and outline share one
                    # colour), so outlines can't be preserved separately; a
                    # dark tint keeps thin lines reading as line art while the
                    # fabric mass shows colour.
                    nr, ng, nb = rgb(nh, 0.8, 0.28 + vv * 0.5)
                else:
                    continue
                px[x, y] = (nr, ng, nb, a)
    return out


def recolour_boots(img, seed):
    """Yellow rubber -> the Soft Suit's orange accent, same formula as the
    suit's own red->orange band recolour, so the built-in boots match."""
    out = img.convert("RGBA")
    px = out.load()
    for y in range(out.height):
        for x in range(out.width):
            r, g, b, a = px[x, y]
            if a <= 10:
                continue
            h, s, v = hsv(r, g, b)
            if s > 0.25 and 0.06 < h < 0.22:
                px[x, y] = rgb(0.055, max(0.65, min(0.95, s)), min(1.0, v * 1.05)) + (a,)
    return out


RECOLOUR = {"eva": recolour_eva, "warm": recolour_warm, "boots": recolour_boots}


def extract(missing):
    """Fetch only the missing source kanims (text + textures) from game assets."""
    import UnityPy
    CACHE.mkdir(parents=True, exist_ok=True)
    wanted_text = set()
    for src in missing:
        wanted_text.add(f"{src}_build")
        if src in JOBS:  # SLIDES sources are build-only, no _anim exists
            wanted_text.add(f"{src}_anim")
    prefixes = tuple(missing)
    need = set(wanted_text) | {f"{s}_0" for s in missing}
    found = {}

    # base-game *.assets first (cheap); only touch big DLC bundles if still short
    targets = sorted(GAME_DATA.glob("*.assets"))
    targets += sorted((GAME_DATA / "StreamingAssets").glob("*bundle*"))
    for assets in targets:
        if need <= set(found):
            break
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
    still = need - set(found)
    if still:
        sys.exit(f"MISSING from game assets: {sorted(still)}")


def generate():
    from PIL import Image
    for src, (new_name, mode) in JOBS.items():
        bd = (CACHE / f"{src}_build.bytes").read_bytes()
        ad = (CACHE / f"{src}_anim.bytes").read_bytes()
        build, anim = parse_build(bd), parse_anim(ad)
        assert write_build(build) == bd, f"{src}: build roundtrip failed"
        assert write_anim(anim) == ad, f"{src}: anim roundtrip failed"

        build["name"] = new_name
        out = ANIM / f"{new_name}_anims" / new_name
        out.mkdir(parents=True, exist_ok=True)
        (out / f"{new_name}_build.bytes").write_bytes(write_build(build))
        (out / f"{new_name}_anim.bytes").write_bytes(write_anim(anim))

        seed = abs(hash(new_name)) % 9999
        for tex in sorted(CACHE.glob(f"{src}_*.png")):
            idx = tex.stem[len(src) + 1:]
            if not idx.isdigit():
                continue
            if mode == "rainbow":
                result = recolour_rainbow(Image.open(tex), build)
            else:
                result = RECOLOUR[mode](Image.open(tex), seed)
            result.save(out / f"{new_name}_{idx}.png")
        print("wrote", out)


def generate_slides():
    """Write the pivot-shifted headgear copies (build-only kanims)."""
    import shutil
    for src, variants in SLIDES.items():
        bd = (CACHE / f"{src}_build.bytes").read_bytes()
        build = parse_build(bd)
        assert write_build(build) == bd, f"{src}: build roundtrip failed"
        for new_name, dy in variants:
            import copy
            b = copy.deepcopy(build)
            b["name"] = new_name
            for sym in b["symbols"]:
                for f in sym["frames"]:
                    # frame layout: [..., pivotX, pivotY, pivotW, pivotH, uvs]
                    f[4] += SLIDE_SIGN * dy * f[6]
            out = ANIM / f"{new_name}_anims" / new_name
            out.mkdir(parents=True, exist_ok=True)
            (out / f"{new_name}_build.bytes").write_bytes(write_build(b))
            for tex in sorted(CACHE.glob(f"{src}_*.png")):
                idx = tex.stem[len(src) + 1:]
                if idx.isdigit():
                    shutil.copyfile(tex, out / f"{new_name}_{idx}.png")
            print("wrote", out)


if __name__ == "__main__":
    missing = [s for s in JOBS if not (CACHE / f"{s}_build.bytes").exists()
               or not (CACHE / f"{s}_anim.bytes").exists()
               or not (CACHE / f"{s}_0.png").exists()]
    missing += [s for s in SLIDES if not (CACHE / f"{s}_build.bytes").exists()
                or not (CACHE / f"{s}_0.png").exists()]
    if missing:
        print("need to extract:", missing)
        extract(missing)
    generate()
    generate_slides()
