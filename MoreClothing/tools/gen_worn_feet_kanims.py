#!/usr/bin/env python3
"""Generates worn-feet builds for the Snazzy Rubber Boots and Shoes.

Run with ~/.venvs/oni-kanim/bin/python (needs Pillow).

NO footwear worn art exists anywhere on the dupe rig: vanilla footwear has
no build override, suit builds cover feet through their `leg` frames, and
the `foot` frames in swimwear-style builds are transparent stubs. So the
boot art here is drawn in code, at foot-bone scale, and each build carries
just a `foot` symbol whose frames all show it:

  body_snazzy_rubber_boots -- tall gold rubber boots
  body_snazzy_shoes        -- low black dress shoes with a gold buckle

The build/anim binary skeleton is borrowed from body_snazzy_swimwear (the
anim banks are never played; only the build's foot symbol matters for worn
overrides). The dupe foot bone geometry: naked foot pivot is
(0.3, 35.6, 19, 20) -- art center 35.6 units below the leg bone, sole at
+45.6. Our boots keep the sole line and grow upward/outward.
"""
import sys
from pathlib import Path

REPO = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(REPO.parent / "MagpieExtensionRonivans" / "tools"))
from gen_extended_kanims import parse_build, write_build

# Borrow the build skeleton (incl. the 'foot' symbol) from the vanilla wetsuit
# cache, NOT the generated body_snazzy_swimwear -- that one now has 'foot'
# stripped so it hides cleanly under suits, which would leave no donor here.
SRC_DIR = REPO / "tools" / "vanilla_kanim_cache"
SRC = "body_wetsuit"

GOLD = (222, 178, 55)
GOLD_DARK = (168, 128, 30)
BLACK = (38, 36, 40)
BLACK_HI = (72, 70, 78)
SOLE = (30, 24, 20)
OUTLINE = (25, 20, 16)

# world-unit pivot for the drawn boots: sole stays at the naked foot's
# sole line (+45.6 below the leg bone), body grows up and out
BOOT_PIVOT = (0.3, 28.6, 30.0, 34.0)   # tall rubber boot
SHOE_PIVOT = (0.3, 33.6, 28.0, 24.0)   # low dress shoe


def _finish(fill, S, W, H):
    from PIL import Image, ImageFilter
    mask = fill.getchannel("A")
    fat = mask.filter(ImageFilter.MaxFilter(2 * S + 1))
    out = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    out.paste(Image.new("RGBA", (W, H), OUTLINE + (255,)), (0, 0), fat)
    out.paste(fill, (0, 0), mask)
    return out


def draw_boot(w=60, h=68):
    """Tall gold rubber boot, toe to the left like the rig's feet."""
    from PIL import Image, ImageDraw
    S = 4
    W, H = w * S, h * S
    fill = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(fill)
    # shaft
    d.rounded_rectangle([0.28 * W, 0.04 * H, 0.86 * W, 0.78 * H],
                        radius=0.10 * W, fill=GOLD)
    # cuff line + ridges
    d.rectangle([0.28 * W, 0.04 * H, 0.86 * W, 0.16 * H], fill=GOLD_DARK)
    for fy in (0.42, 0.58):
        d.line([0.30 * W, fy * H, 0.84 * W, fy * H], fill=GOLD_DARK, width=S)
    # foot / toe
    d.rounded_rectangle([0.06 * W, 0.58 * H, 0.86 * W, 0.90 * H],
                        radius=0.12 * W, fill=GOLD)
    # sole
    d.rounded_rectangle([0.04 * W, 0.82 * H, 0.88 * W, 0.96 * H],
                        radius=0.05 * W, fill=SOLE)
    return _finish(fill, S, W, H).resize((w, h), Image.LANCZOS)


def draw_shoe(w=56, h=48):
    """Low black dress shoe with a gold buckle."""
    from PIL import Image, ImageDraw
    S = 4
    W, H = w * S, h * S
    fill = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(fill)
    d.rounded_rectangle([0.06 * W, 0.28 * H, 0.90 * W, 0.86 * H],
                        radius=0.16 * W, fill=BLACK)
    d.rounded_rectangle([0.50 * W, 0.20 * H, 0.90 * W, 0.60 * H],
                        radius=0.10 * W, fill=BLACK_HI)
    # gold buckle
    d.rectangle([0.58 * W, 0.34 * H, 0.74 * W, 0.52 * H], fill=GOLD)
    # sole
    d.rounded_rectangle([0.04 * W, 0.74 * H, 0.92 * W, 0.92 * H],
                        radius=0.05 * W, fill=SOLE)
    return _finish(fill, S, W, H).resize((w, h), Image.LANCZOS)


JOBS = {
    "body_snazzy_rubber_boots": (draw_boot, BOOT_PIVOT),
    "body_snazzy_shoes": (draw_shoe, SHOE_PIVOT),
}


def generate():
    bd = (SRC_DIR / f"{SRC}_build.bytes").read_bytes()
    ad = (SRC_DIR / f"{SRC}_anim.bytes").read_bytes()

    for name, (draw, pivot) in JOBS.items():
        build = parse_build(bd)
        assert write_build(build) == bd, "roundtrip failed"
        table = dict(build["hashes"])
        kept = [s for s in build["symbols"] if table.get(s["hash"]) == "foot"]
        assert kept, "no foot symbol in donor skeleton"
        build["symbols"] = kept
        build["numSymbols"] = 1
        build["numFrames"] = kept[0]["numFrames"]
        build["name"] = name

        art = draw()
        # every frame shows the same art: full-texture UV, our pivot
        for fr in kept[0]["frames"]:
            fr[3:7] = list(pivot)
            fr[7:] = [0.0, 0.0, 1.0, 1.0]

        out = REPO / "anim" / f"{name}_anims" / name
        out.mkdir(parents=True, exist_ok=True)
        (out / f"{name}_build.bytes").write_bytes(write_build(build))
        (out / f"{name}_anim.bytes").write_bytes(ad)
        art.save(out / f"{name}_0.png")
        print(f"wrote {out} ({kept[0]['numFrames']} foot frames, art {art.size})")


if __name__ == "__main__":
    generate()
