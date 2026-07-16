#!/usr/bin/env python3
"""Builds the Mannequin building's kanim from the vanilla Item Pedestal.

Run with ~/.venvs/oni-kanim/bin/python (needs UnityPy + Pillow).

The pedestal is the donor because the Mannequin IS a re-skinned pedestal
(same 1x2 footprint, same receptacle machinery, same anim states -- the
building def keeps DefaultAnimState "pedestal"). We parse its build, rename
it to "mannequin", and repaint every symbol frame's atlas region with a
tailor's dressform drawn here in code: linen torso on a dark wood pole and
base, ONI-style dark outline, 4x supersampled for soft edges.

Klei build UVs may be measured from either the top or the bottom of the
texture, so the orientation that actually contains the donor's opaque pixels
is detected first (same trick as the rainbow recolour) and the dressform is
drawn in that orientation.

Output goes to anim/mannequin_anims/mannequin/, which build.sh copies into
dist/. Re-run after game updates if Klei changes the pedestal art.
"""
import sys
from pathlib import Path

REPO = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(REPO.parent / "MagpieExtensionRonivans" / "tools"))
from gen_extended_kanims import parse_build, write_build, parse_anim, write_anim

GAME_DATA = Path.home() / ".local/share/Steam/steamapps/common/OxygenNotIncluded/OxygenNotIncluded_Data"
CACHE = REPO / "tools" / "vanilla_kanim_cache"
ANIM = REPO / "anim"

SRC = "pedestal"
NEW = "mannequin"

# palette
LINEN = (208, 181, 150)
LINEN_DARK = (172, 143, 112)
WOOD = (99, 66, 42)
WOOD_DARK = (66, 43, 27)
OUTLINE = (38, 28, 22)


def extract():
    import UnityPy
    CACHE.mkdir(parents=True, exist_ok=True)
    wanted = {f"{SRC}_build", f"{SRC}_anim"}
    found = {}
    targets = sorted(GAME_DATA.glob("*.assets"))
    targets += sorted((GAME_DATA / "StreamingAssets").glob("*bundle*"))
    for assets in targets:
        if wanted <= set(found) and f"{SRC}_0" in found:
            break
        env = UnityPy.load(str(assets))
        for obj in env.objects:
            if obj.type.name not in ("TextAsset", "Texture2D"):
                continue
            data = obj.read()
            name = data.m_Name
            if obj.type.name == "TextAsset" and name in wanted:
                raw = data.m_Script
                if isinstance(raw, str):
                    raw = raw.encode("utf-8", "surrogateescape")
                (CACHE / f"{name}.bytes").write_bytes(raw)
                found[name] = assets.name
            elif obj.type.name == "Texture2D" and name.startswith(SRC + "_") \
                    and name[len(SRC) + 1:].isdigit():
                data.image.save(CACHE / f"{name}.png")
                found[name] = assets.name
    print("extracted:", sorted(found))
    missing = (wanted | {f"{SRC}_0"}) - set(found)
    if missing:
        sys.exit(f"MISSING from game assets: {sorted(missing)}")


def torso_half_width(t):
    """Dressform silhouette half-width (fraction of box width) at torso
    depth t in [0,1]: shoulders -> bust -> pinched waist -> hip flare."""
    keys = [(0.00, 0.10), (0.10, 0.26), (0.30, 0.28), (0.60, 0.18),
            (0.88, 0.26), (1.00, 0.20)]
    for (t0, w0), (t1, w1) in zip(keys, keys[1:]):
        if t0 <= t <= t1:
            f = (t - t0) / (t1 - t0)
            f = f * f * (3 - 2 * f)  # smoothstep
            return w0 + (w1 - w0) * f
    return keys[-1][1]


def draw_dressform(w, h):
    """Return an RGBA image (w x h) of the dressform, drawn 4x supersampled."""
    from PIL import Image, ImageDraw, ImageFilter
    S = 4
    W, H = w * S, h * S
    fill = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(fill)
    cx = W / 2

    # proportions (fractions of box height)
    knob_top, neck_top = 0.02, 0.055
    torso_top, torso_bot = 0.115, 0.66
    base_cy, base_ry = 0.945, 0.038

    # pole
    d.rectangle([cx - 0.022 * W, torso_bot * H, cx + 0.022 * W, base_cy * H], fill=WOOD)
    # base disc
    d.ellipse([cx - 0.30 * W, (base_cy - base_ry) * H,
               cx + 0.30 * W, (base_cy + base_ry) * H], fill=WOOD)
    d.ellipse([cx - 0.30 * W, (base_cy - base_ry) * H,
               cx + 0.30 * W, (base_cy + 0.2 * base_ry) * H], fill=WOOD_DARK)
    # neck + cap knob
    d.rectangle([cx - 0.05 * W, neck_top * H, cx + 0.05 * W, (torso_top + 0.02) * H], fill=LINEN_DARK)
    d.ellipse([cx - 0.045 * W, knob_top * H, cx + 0.045 * W, (knob_top + 0.045) * H], fill=WOOD)

    # torso polygon
    steps = 48
    left, right = [], []
    for i in range(steps + 1):
        t = i / steps
        y = (torso_top + (torso_bot - torso_top) * t) * H
        hw = torso_half_width(t) * W
        left.append((cx - hw, y))
        right.append((cx + hw, y))
    d.polygon(left + right[::-1], fill=LINEN)

    # shading: darken the right third of the torso + a centre seam line
    px = fill.load()
    for yy in range(int(torso_top * H), int(torso_bot * H)):
        t = (yy / H - torso_top) / (torso_bot - torso_top)
        hw = torso_half_width(t) * W
        for xx in range(int(cx), int(cx + hw) + 1):
            r, g, b, a = px[xx, yy]
            if a == 0 or (r, g, b) != LINEN:
                continue
            f = (xx - cx) / max(hw, 1)
            if f > 0.35:
                k = 1.0 - 0.28 * (f - 0.35) / 0.65
                px[xx, yy] = (int(r * k), int(g * k), int(b * k), a)
    d.line([cx, torso_top * H + 2 * S, cx, torso_bot * H - 2 * S], fill=LINEN_DARK, width=S)

    # outline: dilate the alpha mask and paint the rim dark
    mask = fill.getchannel("A")
    fat = mask.filter(ImageFilter.MaxFilter(2 * S + 1))
    outline = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    outline.paste(Image.new("RGBA", (W, H), OUTLINE + (255,)), (0, 0), fat)
    outline.paste(fill, (0, 0), mask)
    return outline.resize((w, h), Image.LANCZOS)


def frame_boxes(build, W, H):
    """Yield (x0, y0, x1, y1) pixel boxes for every symbol frame, in whichever
    vertical orientation actually contains the donor's opaque pixels."""
    raw = []
    for s in build["symbols"]:
        for f in s["frames"]:
            u0, v0, u1, v1 = f[-4:]
            raw.append((int(u0 * W), int(u1 * W), int(v0 * H), int(v1 * H),
                        int((1 - v1) * H), int((1 - v0) * H)))
    return raw


def coverage(px, x0, x1, y0, y1, W, H):
    total = opaque = 0
    for y in range(max(y0, 0), min(y1, H), 2):
        for x in range(max(x0, 0), min(x1, W), 2):
            total += 1
            if px[x, y][3] > 10:
                opaque += 1
    return opaque / max(total, 1)


def generate():
    from PIL import Image
    bd = (CACHE / f"{SRC}_build.bytes").read_bytes()
    ad = (CACHE / f"{SRC}_anim.bytes").read_bytes()
    build, anim = parse_build(bd), parse_anim(ad)
    assert write_build(build) == bd, "build roundtrip failed"
    assert write_anim(anim) == ad, "anim roundtrip failed"

    table = dict(build["hashes"])
    print("donor symbols:", [table.get(s["hash"], s["hash"]) for s in build["symbols"]])

    build["name"] = NEW
    out = ANIM / f"{NEW}_anims" / NEW
    out.mkdir(parents=True, exist_ok=True)
    (out / f"{NEW}_build.bytes").write_bytes(write_build(build))
    (out / f"{NEW}_anim.bytes").write_bytes(write_anim(anim))

    img = Image.open(CACHE / f"{SRC}_0.png").convert("RGBA")
    W, H = img.size
    px = img.load()
    boxes = frame_boxes(build, W, H)
    top = sum(coverage(px, x0, x1, ty0, ty1, W, H) for x0, x1, ty0, ty1, _, _ in boxes)
    bot = sum(coverage(px, x0, x1, fy0, fy1, W, H) for x0, x1, _, _, fy0, fy1 in boxes)
    flipped = bot > top
    print(f"uv orientation: {'bottom-up' if flipped else 'top-down'} "
          f"(coverage {bot:.2f} vs {top:.2f})")

    for x0, x1, ty0, ty1, fy0, fy1 in boxes:
        y0, y1 = (fy0, fy1) if flipped else (ty0, ty1)
        w, h = x1 - x0, y1 - y0
        if w < 4 or h < 4:
            continue
        art = draw_dressform(w, h)
        img.paste(Image.new("RGBA", (w, h), (0, 0, 0, 0)), (x0, y0))
        img.paste(art, (x0, y0), art)

    img.save(out / f"{NEW}_0.png")
    print("wrote", out)


if __name__ == "__main__":
    if not ((CACHE / f"{SRC}_build.bytes").exists()
            and (CACHE / f"{SRC}_anim.bytes").exists()
            and (CACHE / f"{SRC}_0.png").exists()):
        extract()
    generate()
