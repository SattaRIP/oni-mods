#!/usr/bin/env python3
"""Widen the single-image electric bridge kanims (round end-terminals + a wavy
middle wire) WITHOUT distorting the terminals and WITHOUT repacking the atlas.

Why this exists: generate_scaled() uniformly scales the whole bridge image, which
ovals the round end-terminals AND fails to push them out to the wide building's
socket cells (the discrete bulbs reveal the misalignment that the continuous
liquid/gas pipe art hides). The earlier widen_wire_kanims.py fixed the look but
REPACKED the atlas into a new power-of-2 texture + rewrote all UVs, which was the
suspected trigger of an intermittent launch crash on this install.

This generator keeps the ORIGINAL _0.png byte-identical (no repack) and instead:
  * adds three sub-rectangle FRAMES to the bridge symbol — left cap, middle wire,
    right cap — each a UV slice of the unchanged texture, and
  * rewrites each non-'ui' anim to place those three as separate elements: the
    two caps at NATIVE size centred on the building's socket cells, and the
    middle wire stretched (ma) to bridge the gap between them.
This is the same multi-element composition the Ronivans hpa_rail_tile_bridge
generator already uses successfully (one element per cell, original atlas reused).

Socket cells: a W-wide bridge is centred on its footprint, so its outer (socket)
cells sit at +/-(W-1)/2 cells from the anim origin. We centre each cap there.
"""
import sys, copy
from pathlib import Path
from PIL import Image
sys.path.insert(0, "/home/mythraps/Documents/ONI_Mods/MagpieExtensionRonivans/tools")
import gen_extended_kanims as g

REPO = Path(__file__).resolve().parent.parent
CACHE = REPO / "tools" / "vanilla_kanim_cache"
OUT = REPO / "anim" / "magpie_extended_anims"

PX_PER_CELL = 200.0      # logical units per grid cell (matches Ronivans generator)
ATLAS_TO_LOGICAL = 2.0   # art stored at half-res: logical px = atlas px * 2
CAP_FRAC = 0.40          # outer fraction of the image kept as each end cap (native)


def _frame_uv_px(fr, AW, AH):
    # symbol frame floats: pivotX,pivotY,pivotW,pivotH,x1,y1,x2,y2
    pivX, pivY, pivW, pivH, x1, y1, x2, y2 = fr[3:]
    return pivX, pivY, pivW, pivH, x1, y1, x2, y2


def _slice_frame(orig, src_int2, AW, frame_index, ux1, ux2, logical_w):
    """Build a new symbol frame that is a horizontal sub-rectangle [ux1,ux2]
    (normalized U) of the original frame, with pivot at the slice's own centre."""
    pivX, pivY, pivW, pivH, x1, y1, x2, y2 = orig[3:]
    # ints: [sourceframenum, duration, buildimageindex]
    return [frame_index, orig[1], orig[2],
            0.0, pivY, logical_w, pivH,
            ux1, y1, ux2, y2]


def generate(base, width):
    build, anim, _ = g.load_validated(CACHE / base, base)
    atlas_png = CACHE / base / f"{base}_0.png"
    AW, AH = Image.open(atlas_png).size
    name = f"{base}{width}"

    socket_x = (width - 1) / 2.0 * PX_PER_CELL   # outer cell centre, logical px

    nb = copy.deepcopy(build)
    nb['name'] = name
    na = copy.deepcopy(anim)
    na['name'] = name

    # index symbols by hash for quick frame appends
    sym_by_hash = {s['hash']: s for s in nb['symbols']}

    total_elements = 0
    for an in na['anims']:
        if an['name'] == 'ui':
            for fr in an['frames']:
                total_elements += len(fr['elements'])
            continue
        # each non-ui anim is a single full-bridge element; slice its symbol
        sample = an['frames'][0]['elements']
        assert len(sample) == 1, f"{base}/{an['name']}: expected 1 element, got {len(sample)}"
        el = sample[0]
        sym = sym_by_hash[el['symbolHash']]
        of = sym['frames'][0]                     # original full-image frame
        full_pivW = of[5]                          # logical width
        # frame layout: [i0,i1,i2, pivotX(3),pivotY(4),pivotW(5),pivotH(6), x1(7),y1(8),x2(9),y2(10)]
        u1, u2 = of[7], of[9]                      # atlas U extent of the full image
        uw = u2 - u1
        # slice U boundaries
        uL = u1 + CAP_FRAC * uw
        uR = u1 + (1.0 - CAP_FRAC) * uw
        left_lw = CAP_FRAC * full_pivW
        mid_lw = (1.0 - 2 * CAP_FRAC) * full_pivW
        right_lw = CAP_FRAC * full_pivW
        # append three frames (indices 1,2,3) to this symbol if not already done
        base_idx = len(sym['frames'])
        f_left = _slice_frame(of, None, AW, base_idx + 0, u1, uL, left_lw)
        f_mid = _slice_frame(of, None, AW, base_idx + 1, uL, uR, mid_lw)
        f_right = _slice_frame(of, None, AW, base_idx + 2, uR, u2, right_lw)
        sym['frames'] += [f_left, f_mid, f_right]
        sym['numFrames'] = len(sym['frames'])

        # placement of the three slices
        left_cap_half = left_lw / 2.0
        mid_target_w = 2 * socket_x - 2 * left_cap_half
        mid_ma = mid_target_w / mid_lw

        def mk(frame_num, tx, ma, order):
            e = copy.deepcopy(el)
            e['frameNum'] = frame_num
            e['tx'] = tx
            e['ma'] = ma
            e['order'] = float(order)
            return e

        for fr in an['frames']:
            fr['elements'] = [
                mk(base_idx + 1, 0.0, mid_ma, 0),         # middle wire (behind)
                mk(base_idx + 0, -socket_x, 1.0, 1),      # left cap (on top)
                mk(base_idx + 2, +socket_x, 1.0, 1),      # right cap (on top)
            ]
            total_elements += 3

    na['h_frames'] = total_elements
    na['maxVisSymbolFrames'] = max(s['numFrames'] for s in nb['symbols'])

    out = OUT / name
    out.mkdir(parents=True, exist_ok=True)
    (out / f"{name}_build.bytes").write_bytes(g.write_build(nb))
    (out / f"{name}_anim.bytes").write_bytes(g.write_anim(na))
    import shutil
    shutil.copy(atlas_png, out / f"{name}_0.png")   # original atlas, unchanged
    print(f"wrote {name}: socket_x={socket_x:.0f} mid_ma={mid_ma:.2f} elements={total_elements}")


def main():
    bases = sys.argv[1:] or [
        "utilityelectricbridge",
        "utilityelectricbridgeconductive",
        "utilityelectricbridgerubber",
    ]
    for b in bases:
        for w in (4, 5):
            generate(b, w)


if __name__ == '__main__':
    main()
