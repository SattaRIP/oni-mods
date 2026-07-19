#!/usr/bin/env python3
"""
Generates wide (2/3-cell) Heavi-Watt / Conductive Joint Plate kanims from the
vanilla heavywatttile art in tools/vanilla_kanim_cache/, by per-cell element
composition (no stretching, no atlas rewrite -- same safe technique as the
Ronivans hpa_rail_tile_bridge widener):

  - one 'tile_fg' face element per cell, listed first (drawn in front)
  - the full 'outlets' socket composite on each END cell, behind the faces,
    so only the outward-facing sockets show past the plate
  - 'place' recomposed from the same face+outlets layout (vanilla uses a
    dedicated 584px 'place' symbol that only fits 1x1)
  - 'ui' untouched

Also emits insulated recolors of the 3-wide variants (our own palette
transform of the vanilla art -- no third-party assets) for the variants that
pair with the "Insulated Joint Plate [FIXED]" mod.

Writes into <repo>/anim/magpie_extended_anims/. Run via build.sh.
"""
import copy
import shutil
import sys
from pathlib import Path

REPO = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(REPO.parent / "MagpieExtensionRonivans" / "tools"))
import gen_extended_kanims as g

CACHE = REPO / "tools" / "vanilla_kanim_cache"
OUT_BASE = REPO / "anim" / "magpie_extended_anims"
PX_PER_CELL = 200.0


def hash_of(anim, name):
    for h, n in anim['hashes']:
        if n == name:
            return h
    raise KeyError(name)


def compose(base, width, out_name, png_path):
    build, anim, _ = g.load_validated(CACHE / base, base)

    nb = copy.deepcopy(build)
    nb['name'] = out_name

    na = copy.deepcopy(anim)
    face_h = hash_of(anim, 'tile_fg')
    out_h = hash_of(anim, 'outlets')
    half = (width - 1) / 2.0
    cells = [k - half for k in range(width)]

    # Templates: the 'on' anim's face + outlets elements carry the exact
    # transforms/colors; 'off' differs only in the face frameNum.
    def templates(anim_name):
        src = next(an for an in anim['anims'] if an['name'] == ('off' if anim_name == 'off' else 'on'))
        els = src['frames'][0]['elements']
        face = next(e for e in els if e['symbolHash'] == face_h)
        outlets = next(e for e in els if e['symbolHash'] == out_h)
        return face, outlets

    def layout(anim_name):
        face_tpl, out_tpl = templates(anim_name)
        new = []
        for c in cells:  # faces on every cell, drawn in front (listed first)
            e = copy.deepcopy(face_tpl)
            e['tx'] = face_tpl['tx'] + PX_PER_CELL * c
            new.append(e)
        for c in (cells[0], cells[-1]):  # socket composites behind, ends only
            e = copy.deepcopy(out_tpl)
            e['tx'] = out_tpl['tx'] + PX_PER_CELL * c
            new.append(e)
        return new

    total = 0
    for an in na['anims']:
        for fr in an['frames']:
            if an['name'] in ('on', 'off', 'place'):
                fr['elements'] = layout(an['name'])
            total += len(fr['elements'])
    na['h_frames'] = total  # header tracks total elements
    na['maxVisSymbolFrames'] = width + 2

    out = OUT_BASE / out_name
    out.mkdir(parents=True, exist_ok=True)
    (out / f"{out_name}_build.bytes").write_bytes(g.write_build(nb))
    (out / f"{out_name}_anim.bytes").write_bytes(g.write_anim(na))
    shutil.copy(png_path, out / f"{out_name}_0.png")
    print(f"wrote {out} (elements={total}, cells={cells})")


def insulated_png(base):
    """Own insulated recolor: low-saturation (bare metal) pixels shift to the
    warm tan of insulated tiles; saturated pixels (hazard striping, copper)
    keep their color so the sockets stay recognizable."""
    from PIL import Image
    src = Image.open(CACHE / base / f"{base}_0.png").convert('RGBA')
    px = src.load()
    w, h = src.size
    for y in range(h):
        for x in range(w):
            r, gch, b, a = px[x, y]
            if a == 0:
                continue
            mx, mn = max(r, gch, b), min(r, gch, b)
            sat = 0 if mx == 0 else (mx - mn) / mx
            if sat < 0.25:  # grayish plate metal
                lum = (r + gch + b) / 3.0
                px[x, y] = (min(255, int(lum * 1.02)),
                            min(255, int(lum * 0.80)),
                            min(255, int(lum * 0.52)), a)
    out = CACHE / base / f"{base}_ins.png"
    src.save(out)
    return out


def main():
    for base in ("heavywatttile", "heavywatttile_conductive"):
        png = CACHE / base / f"{base}_0.png"
        for w in (2, 3):
            compose(base, w, f"{base}{w}", png)
        compose(base, 3, f"{base}_ins3", insulated_png(base))


if __name__ == '__main__':
    main()
