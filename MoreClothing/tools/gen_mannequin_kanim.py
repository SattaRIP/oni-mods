#!/usr/bin/env python3
"""Builds the Mannequin building's kanim from the vanilla Item Pedestal.

Run with ~/.venvs/oni-kanim/bin/python (needs UnityPy + Pillow).

The pedestal is the donor because the Mannequin IS a re-skinned pedestal
(same 1x2 footprint, same receptacle machinery, same anim states -- the
building def keeps DefaultAnimState "pedestal"). We parse its build, rename
it to "mannequin", and rebuild the atlas with a tailor's dressform drawn
here in code: linen torso on a dark wood pole and base, ONI-style dark
outline, 4x supersampled for soft edges.

Key geometry facts (dumped from the donor):
  * The donor's built-state symbol `pedestal_item` is only 288x276 anim
    units (~1.4 tiles) -- the pedestal is a short stand -- while its `place`
    symbol is 288x480 (~2.4 tiles, the full footprint). Drawing into the
    donor boxes as-is makes the BUILT mannequin a tile shorter than its
    blueprint, so the built frame instead copies `place`'s pivot geometry
    (and its anim element transform) and gets a full-size 144x240 region.
  * Vanilla blueprints are white because the ghost look is baked into the
    `place` art -- and it's white LINE ART with a transparent interior (the
    donor's is a hand-drawn white outline of the pedestal), so the
    mannequin's `place` is drawn as hollow white line-art.
  * A `torso` symbol (Klei SDBM hash of "torso", same symbol name the dupe
    rig uses) is added: a transparent placeholder that MannequinDecor
    overrides at runtime with the displayed garment's worn `torso` art via
    SymbolOverrideController -- the mannequin literally wears the clothes.
    Its anim element uses frameNum 10 (the front-facing view on the dupe
    rig; present in every worn build checked) and is calibrated below from
    the worn-torso pivot so the garment lands on the dressform's torso.

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

# Klei SDBM hash of "torso" (matches the dupe rig / worn clothing builds).
TORSO_HASH = 740660083

# Worn-garment torso element calibration, in anim units (200 units = 1 tile,
# y-down). Target = the dressform torso's box inside the 288x480 place-sized
# art (torso spans 0.115..0.66 of the box height; art center from the donor
# place frame: element t + pivot = (-6.5, -199.3), so the art top edge is at
# -439.3). Source = worn `torso` frame 10 pivot, x=0.46 y=-55.24 w=88 h=110
# (identical across snazzy swimwear / winter coat / soft suit worn builds).
TORSO_SCALE = 2.3
TORSO_CENTER = (-6.5, -253.3)
TORSO_SRC_PIVOT = (0.46, -55.24)
TORSO_TX = TORSO_CENTER[0] - TORSO_SCALE * TORSO_SRC_PIVOT[0]
TORSO_TY = TORSO_CENTER[1] - TORSO_SCALE * TORSO_SRC_PIVOT[1]

# palette
SOLID = {
    'LINEN': (208, 181, 150), 'LINEN_DARK': (172, 143, 112),
    'WOOD': (99, 66, 42), 'WOOD_DARK': (66, 43, 27),
    'OUTLINE': (38, 28, 22),
}
# Blueprint line colour. Vanilla place art (checked on the donor) is white
# LINE ART with a hollow/transparent interior, not a filled silhouette.
GHOST_WHITE = (235, 235, 235, 255)


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


# proportions (fractions of box height), shared by solid + ghost renders
KNOB_TOP, NECK_TOP = 0.02, 0.055
TORSO_TOP, TORSO_BOT = 0.115, 0.66
BASE_CY, BASE_RY = 0.945, 0.038


def torso_outline_points(W, H, cx):
    """Left and right torso edge polylines at supersampled resolution."""
    steps = 48
    left, right = [], []
    for i in range(steps + 1):
        t = i / steps
        y = (TORSO_TOP + (TORSO_BOT - TORSO_TOP) * t) * H
        hw = torso_half_width(t) * W
        left.append((cx - hw, y))
        right.append((cx + hw, y))
    return left, right


def draw_dressform_ghost(w, h):
    """White line-art dressform (hollow interior), like vanilla place art."""
    from PIL import Image, ImageDraw
    S = 4
    W, H = w * S, h * S
    img = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(img)
    cx = W / 2
    lw = int(1.6 * S)

    left, right = torso_outline_points(W, H, cx)
    d.line(left, fill=GHOST_WHITE, width=lw, joint="curve")
    d.line(right, fill=GHOST_WHITE, width=lw, joint="curve")
    d.line([left[0], right[0]], fill=GHOST_WHITE, width=lw)      # shoulders
    d.line([left[-1], right[-1]], fill=GHOST_WHITE, width=lw)    # hem
    d.line([cx, TORSO_TOP * H + 2 * S, cx, TORSO_BOT * H - 2 * S],
           fill=GHOST_WHITE, width=max(S, lw // 2))              # seam

    # pole
    d.rectangle([cx - 0.022 * W, TORSO_BOT * H, cx + 0.022 * W, BASE_CY * H],
                outline=GHOST_WHITE, width=lw)
    # base disc
    d.ellipse([cx - 0.30 * W, (BASE_CY - BASE_RY) * H,
               cx + 0.30 * W, (BASE_CY + BASE_RY) * H],
              outline=GHOST_WHITE, width=lw)
    # neck + cap knob
    d.rectangle([cx - 0.05 * W, NECK_TOP * H, cx + 0.05 * W, (TORSO_TOP + 0.02) * H],
                outline=GHOST_WHITE, width=lw)
    d.ellipse([cx - 0.045 * W, KNOB_TOP * H, cx + 0.045 * W, (KNOB_TOP + 0.045) * H],
              outline=GHOST_WHITE, width=lw)

    return img.resize((w, h), Image.LANCZOS)


def draw_dressform(w, h, pal):
    """Return an RGBA image (w x h) of the dressform, drawn 4x supersampled."""
    from PIL import Image, ImageDraw, ImageFilter
    LINEN, LINEN_DARK = pal['LINEN'], pal['LINEN_DARK']
    WOOD, WOOD_DARK, OUTLINE = pal['WOOD'], pal['WOOD_DARK'], pal['OUTLINE']
    S = 4
    W, H = w * S, h * S
    fill = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(fill)
    cx = W / 2

    # proportions (fractions of box height)
    knob_top, neck_top = KNOB_TOP, NECK_TOP
    torso_top, torso_bot = TORSO_TOP, TORSO_BOT
    base_cy, base_ry = BASE_CY, BASE_RY

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


def generate():
    from PIL import Image
    bd = (CACHE / f"{SRC}_build.bytes").read_bytes()
    ad = (CACHE / f"{SRC}_anim.bytes").read_bytes()
    build, anim = parse_build(bd), parse_anim(ad)
    assert write_build(build) == bd, "build roundtrip failed"
    assert write_anim(anim) == ad, "anim roundtrip failed"

    table = dict(build["hashes"])
    sym = {table.get(s["hash"], s["hash"]): s for s in build["symbols"]}
    place_s, item_s, ui_s = sym["place"], sym["pedestal_item"], sym["ui"]
    print("donor symbols:", list(sym))

    build["name"] = NEW
    out = ANIM / f"{NEW}_anims" / NEW
    out.mkdir(parents=True, exist_ok=True)

    # --- atlas: our own layout (frame layout in build data is updated to
    # match), donor confirmed top-down UVs.
    W, H = 512, 256
    atlas = Image.new("RGBA", (W, H), (0, 0, 0, 0))

    def uv(x0, y0, x1, y1):
        return [x0 / W, y0 / H, x1 / W, y1 / H]

    def put(img, x0, y0):
        atlas.paste(img, (x0, y0), img)

    # place: white line-art ghost (blueprint + under-construction look)
    put(draw_dressform_ghost(144, 240), 0, 0)
    place_s["frames"][0][7:] = uv(0, 0, 144, 240)

    # pedestal_item (built state): full-colour dressform, and the WORLD
    # geometry (pivot) copied from `place` so built == blueprint size.
    put(draw_dressform(144, 240, SOLID), 144, 0)
    item_s["frames"][0][3:7] = list(place_s["frames"][0][3:7])
    item_s["frames"][0][7:] = uv(144, 0, 288, 240)

    # ui (build-menu icon): colour dressform in its original box shape
    put(draw_dressform(108, 148, SOLID), 288, 0)
    ui_s["frames"][0][7:] = uv(288, 0, 396, 148)

    # torso: transparent placeholder; sourceFrameNum 10 so the element's
    # frameNum resolves both here and in the worn builds that override it.
    build["symbols"].append({
        "hash": TORSO_HASH, "path": item_s["path"], "color": item_s["color"],
        "flags": item_s["flags"], "numFrames": 1,
        "frames": [[10, 1, 0, 0.0, 0.0, 16.0, 16.0] + uv(400, 0, 416, 16)],
    })
    build["numSymbols"] += 1
    build["numFrames"] += 1
    build["hashes"].append((TORSO_HASH, "torso"))

    # --- anim: built state gets place's transform + a garment torso element
    n_frames = sum(len(a["frames"]) for a in anim["anims"])
    n_elements = sum(len(f["elements"]) for a in anim["anims"] for f in a["frames"])
    assert anim["h_frames"] == n_frames and anim["h_elements"] == n_elements, \
        (anim["h_frames"], n_frames, anim["h_elements"], n_elements)

    ped = next(a for a in anim["anims"] if a["name"] == "pedestal")
    pla = next(a for a in anim["anims"] if a["name"] == "place")
    pel = pla["frames"][0]["elements"][0]
    prect = pla["frames"][0]["rect"]
    item_hash = item_s["hash"]

    added = 0
    for fr in ped["frames"]:
        fr["rect"] = list(prect)
        base = None
        for e in fr["elements"]:
            if e["symbolHash"] == item_hash:
                e["tx"], e["ty"] = pel["tx"], pel["ty"]
                base = e
        assert base is not None
        torso = dict(base)
        torso["symbolHash"] = TORSO_HASH
        if torso["folderHash"] == item_hash:
            torso["folderHash"] = TORSO_HASH
        torso["frameNum"] = 10
        torso["ma"], torso["mb"], torso["mc"], torso["md"] = TORSO_SCALE, 0.0, 0.0, TORSO_SCALE
        torso["tx"], torso["ty"] = TORSO_TX, TORSO_TY
        fr["elements"].insert(0, torso)  # listed first = drawn in front
        added += 1
    anim["h_elements"] += added
    anim["maxVisSymbolFrames"] = max(anim["maxVisSymbolFrames"], 2)
    anim["hashes"].append((TORSO_HASH, "torso"))
    print(f"torso element: scale={TORSO_SCALE} t=({TORSO_TX:.2f},{TORSO_TY:.2f}) "
          f"folderHash={'retargeted' if torso['folderHash'] == TORSO_HASH else torso['folderHash']}")

    (out / f"{NEW}_build.bytes").write_bytes(write_build(build))
    (out / f"{NEW}_anim.bytes").write_bytes(write_anim(anim))
    atlas.save(out / f"{NEW}_0.png")
    print("wrote", out)


if __name__ == "__main__":
    if not ((CACHE / f"{SRC}_build.bytes").exists()
            and (CACHE / f"{SRC}_anim.bytes").exists()
            and (CACHE / f"{SRC}_0.png").exists()):
        extract()
    generate()
