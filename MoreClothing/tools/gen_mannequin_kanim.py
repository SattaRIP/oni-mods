#!/usr/bin/env python3
"""Builds the Mannequin building's kanim: a dupe-shaped tailor's dummy.

Run with ~/.venvs/oni-kanim/bin/python (needs UnityPy + Pillow).

The building is a re-skinned Item Pedestal (same 1x2 footprint, receptacle
machinery, and anim state names -- the donor build/anim binary structure is
kept and edited). The visible art is rebuilt as a linen dupe torso+pelvis
on a wooden stand:

  * The trunk uses the REAL dupe rig: torso/pelvis art comes from the
    vanilla naked body build (body_comp_default), recoloured to linen, and
    the anim elements copy the element matrices of dupe idle pose
    anim_idles_default/idle_default frame 0 verbatim (shifted onto the
    stand). Because pivots, frame numbers, and transforms are all native
    dupe-rig values, when MannequinDecor overrides the torso/pelvis symbols
    with a garment's worn build the clothing renders EXACTLY as it does on
    an idle dupe.
  * `place` is white line-art with a hollow interior, like vanilla
    blueprints (the ghost look is baked into place art, verified on the
    donor pedestal).
  * Geometry model (dumped from donors): art center (y-down anim units,
    200 units/tile) = element t + M*(pivot x,y); art size = pivot w,h;
    the donor place frame maps a 288x480-unit box centered at
    (-6.5,-199.3) over the full footprint (floor at ~ y=+41).

Output goes to anim/mannequin_anims/mannequin/, which build.sh copies into
dist/. Re-run after game updates if Klei changes the donor art.
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
BODY = "body_comp_default"
IDLE = "anim_idles_default"

# dupe rig body parts shown on the dummy (and overridable by worn builds),
# in idle_default draw order (torso in front of pelvis)
PARTS = ["torso", "pelvis"]

# Shift applied to the copied idle-pose transforms: lifts the trunk onto the
# stand and centers it on the footprint (dupe-space origin is at its feet).
DELTA = (-3.6, -70.0)

# stand palette
WOOD = (99, 66, 42)
WOOD_DARK = (66, 43, 27)
OUTLINE = (38, 28, 22)
LINEN = (208, 181, 150)
GHOST_WHITE = (235, 235, 235, 255)

# world box the place/pedestal_item frames map to (donor place geometry)
BOX_C = (-6.5, -199.3)
BOX_W, BOX_H = 288.0, 480.0

# stand geometry in world units (y-down): base disc on the floor, pole up
# to the trunk (pelvis bottom sits around y=-105 after DELTA)
BASE_CY, BASE_RX, BASE_RY = 18.0, 82.0, 15.0
POLE_TOP, POLE_HW = -125.0, 7.0


def sdbm(s):
    h = 0
    for c in s.lower():
        h = (ord(c) + (h << 6) + (h << 16) - h) & 0xFFFFFFFF
    return h - 0x100000000 if h >= 0x80000000 else h


def extract():
    import UnityPy
    CACHE.mkdir(parents=True, exist_ok=True)
    wanted = {f"{SRC}_build", f"{SRC}_anim", f"{BODY}_build", f"{IDLE}_anim"}
    tex = {f"{SRC}_0": SRC + "_", f"{BODY}_0": BODY + "_"}
    found = {}
    targets = sorted(GAME_DATA.glob("*.assets"))
    targets += sorted((GAME_DATA / "StreamingAssets").glob("*bundle*"))
    for assets in targets:
        if wanted <= set(found) and set(tex) <= set(found):
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
                found[name] = 1
            elif obj.type.name == "Texture2D" and name in tex:
                data.image.save(CACHE / f"{name}.png")
                found[name] = 1
    print("extracted:", sorted(found))
    missing = (wanted | set(tex)) - set(found)
    if missing:
        sys.exit(f"MISSING from game assets: {sorted(missing)}")


def linenize(img):
    """Recolour a dupe body-part crop to linen (keep shading, drop hue)."""
    px = img.load()
    lr, lg, lb = LINEN
    for y in range(img.height):
        for x in range(img.width):
            r, g, b, a = px[x, y]
            if a == 0:
                continue
            v = (0.299 * r + 0.587 * g + 0.114 * b) / 255.0
            v = 0.35 + 0.75 * v  # lift shadows so it reads as pale fabric
            px[x, y] = (min(255, int(lr * v)), min(255, int(lg * v)),
                        min(255, int(lb * v)), a)
    return img


class Piece:
    """One body part: art crop + its build-frame pivot + anim transform."""
    def __init__(self, name, art, pivot, el):
        self.name, self.art, self.pivot, self.el = name, art, pivot, el

    def world_corners(self):
        x, y, w, h = self.pivot
        a, b, c, d = self.el['ma'], self.el['mb'], self.el['mc'], self.el['md']
        tx, ty = self.el['tx'] + DELTA[0], self.el['ty'] + DELTA[1]
        pts = []
        for px_, py_ in ((x - w / 2, y - h / 2), (x + w / 2, y - h / 2),
                         (x + w / 2, y + h / 2), (x - w / 2, y + h / 2)):
            pts.append((a * px_ + c * py_ + tx, b * px_ + d * py_ + ty))
        return pts


def world_to_box(px_w, px_h):
    """Return fn mapping world (y-down units) -> pixel coords in a target
    image of px_w x px_h that covers BOX world rect."""
    x0 = BOX_C[0] - BOX_W / 2
    y0 = BOX_C[1] - BOX_H / 2
    sx, sy = px_w / BOX_W, px_h / BOX_H
    return lambda wx, wy: ((wx - x0) * sx, (wy - y0) * sy)


def solve_affine(src, dst):
    """Solve the 2x3 affine mapping src->dst from 3 point pairs, returned in
    PIL's (a, b, c, d, e, f) order: x' = a*x + b*y + c, y' = d*x + e*y + f."""
    (x0, y0), (x1, y1), (x2, y2) = src
    det = (x1 - x0) * (y2 - y0) - (x2 - x0) * (y1 - y0)
    def row(v0, v1, v2):
        a = ((v1 - v0) * (y2 - y0) - (v2 - v0) * (y1 - y0)) / det
        b = ((v2 - v0) * (x1 - x0) - (v1 - v0) * (x2 - x0)) / det
        c = v0 - a * x0 - b * y0
        return a, b, c
    ax, bx, cx = row(dst[0][0], dst[1][0], dst[2][0])
    ay, by, cy = row(dst[0][1], dst[1][1], dst[2][1])
    return (ax, bx, cx, ay, by, cy)


def composite_trunk(canvas, pieces, to_px):
    """Affine-render each piece's art into canvas (in list order = front
    first, so paste back-to-front = reversed)."""
    from PIL import Image
    for piece in reversed(pieces):
        w_art, h_art = piece.art.size
        corners = [to_px(*p) for p in piece.world_corners()]
        # inverse affine: canvas px -> art px, from 3 corner pairs
        inv = solve_affine(corners[:3], [(0, 0), (w_art, 0), (w_art, h_art)])
        layer = piece.art.transform(canvas.size, Image.AFFINE, inv,
                                    resample=Image.BILINEAR)
        canvas.alpha_composite(layer)


def draw_stand(d, to_px, outline_only=False):
    """Draw pole + base disc with world-space coords onto an ImageDraw."""
    lw = max(2, int(d._img.size[1] / 120)) if outline_only else 0
    x0, y0 = to_px(BOX_C[0] - POLE_HW, POLE_TOP)
    x1, y1 = to_px(BOX_C[0] + POLE_HW, BASE_CY)
    bx0, by0 = to_px(BOX_C[0] - BASE_RX, BASE_CY - BASE_RY)
    bx1, by1 = to_px(BOX_C[0] + BASE_RX, BASE_CY + BASE_RY)
    if outline_only:
        d.rectangle([x0, y0, x1, y1], outline=GHOST_WHITE, width=lw)
        d.ellipse([bx0, by0, bx1, by1], outline=GHOST_WHITE, width=lw)
    else:
        d.rectangle([x0, y0, x1, y1], fill=WOOD, outline=OUTLINE, width=2)
        d.ellipse([bx0, by0, bx1, by1], fill=WOOD, outline=OUTLINE, width=2)
        d.ellipse([bx0, by0, bx1, (by0 + by1) / 2 + 1], fill=WOOD_DARK)


def generate():
    from PIL import Image, ImageDraw, ImageFilter

    bd = (CACHE / f"{SRC}_build.bytes").read_bytes()
    ad = (CACHE / f"{SRC}_anim.bytes").read_bytes()
    build, anim = parse_build(bd), parse_anim(ad)
    assert write_build(build) == bd, "build roundtrip failed"
    assert write_anim(anim) == ad, "anim roundtrip failed"

    # --- dupe rig data: body art + idle pose
    body = parse_build((CACHE / f"{BODY}_build.bytes").read_bytes())
    body_img = Image.open(CACHE / f"{BODY}_0.png").convert("RGBA")
    idle = parse_anim((CACHE / f"{IDLE}_anim.bytes").read_bytes())
    idle_table = dict(idle['hashes'])
    body_table = dict(body['hashes'])
    bank = next(a for a in idle['anims'] if a['name'] == 'idle_default')
    idle_els = {}
    for e in bank['frames'][0]['elements']:
        nm = idle_table.get(e['symbolHash'])
        if nm in PARTS and nm not in idle_els:
            idle_els[nm] = e

    BW, BH = body_img.size
    pieces = []
    for part in PARTS:
        el = idle_els[part]
        sym = next(s for s in body['symbols'] if body_table.get(s['hash']) == part)
        fr = next(f for f in sym['frames']
                  if f[0] <= el['frameNum'] < f[0] + f[1])
        x, y, w, h, u0, v0, u1, v1 = fr[3:]
        crop = body_img.crop((int(u0 * BW), int(v0 * BH),
                              int(u1 * BW), int(v1 * BH)))
        pieces.append(Piece(part, linenize(crop), (x, y, w, h), el))
        print(f"{part}: idle f={el['frameNum']} pivot=({x:.1f},{y:.1f},{w:.0f},{h:.0f}) "
              f"px={crop.size}")

    # --- atlas layout (512x256, top-down UVs like the donor)
    W, H = 512, 256
    atlas = Image.new("RGBA", (W, H), (0, 0, 0, 0))

    def uv(x0, y0, x1, y1):
        return [x0 / W, y0 / H, x1 / W, y1 / H]

    table = dict(build["hashes"])
    sym = {table.get(s["hash"], s["hash"]): s for s in build["symbols"]}
    place_s, item_s, ui_s = sym["place"], sym["pedestal_item"], sym["ui"]
    place_pivot = list(place_s["frames"][0][3:7])

    # pedestal_item (built stand, trunk comes from live symbols): stand only
    stand = Image.new("RGBA", (144, 240), (0, 0, 0, 0))
    ds = ImageDraw.Draw(stand)
    ds._img = stand
    draw_stand(ds, world_to_box(144, 240))
    atlas.paste(stand, (144, 0), stand)
    item_s["frames"][0][3:7] = place_pivot
    item_s["frames"][0][7:] = uv(144, 0, 288, 240)

    # place: hollow white line-art ghost of stand + trunk contour
    naked = Image.new("RGBA", (288, 480), (0, 0, 0, 0))
    composite_trunk(naked, pieces, world_to_box(288, 480))
    from PIL import ImageChops
    mask = naked.getchannel("A").point(lambda a: 255 if a > 40 else 0)
    fat = mask.filter(ImageFilter.MaxFilter(7))
    ring = ImageChops.subtract(fat, mask)
    ghost_big = Image.new("RGBA", (288, 480), (0, 0, 0, 0))
    ghost_big.paste(Image.new("RGBA", (288, 480), GHOST_WHITE), (0, 0), ring)
    dg = ImageDraw.Draw(ghost_big)
    dg._img = ghost_big
    draw_stand(dg, world_to_box(288, 480), outline_only=True)
    ghost = ghost_big.resize((144, 240), Image.LANCZOS)
    atlas.paste(ghost, (0, 0), ghost)
    place_s["frames"][0][7:] = uv(0, 0, 144, 240)

    # ui icon: colour render of the full dummy
    full = Image.new("RGBA", (288, 480), (0, 0, 0, 0))
    df = ImageDraw.Draw(full)
    df._img = full
    draw_stand(df, world_to_box(288, 480))
    composite_trunk(full, pieces, world_to_box(288, 480))
    bbox = full.getbbox()
    icon_src = full.crop(bbox)
    icon = Image.new("RGBA", (108, 148), (0, 0, 0, 0))
    scale = min(108 / icon_src.width, 148 / icon_src.height)
    tw, th = int(icon_src.width * scale), int(icon_src.height * scale)
    icon.paste(icon_src.resize((tw, th), Image.LANCZOS),
               ((108 - tw) // 2, (148 - th) // 2))
    atlas.paste(icon, (288, 0), icon)
    ui_s["frames"][0][7:] = uv(288, 0, 396, 148)

    # torso/pelvis placeholder symbols: the linen art at native dupe pivots
    px_cursor = 396
    for piece in pieces:
        el = idle_els[piece.name]
        pw, ph = piece.art.size
        atlas.paste(piece.art, (px_cursor, 0), piece.art)
        build["symbols"].append({
            "hash": sdbm(piece.name), "path": item_s["path"],
            "color": item_s["color"], "flags": item_s["flags"],
            "numFrames": 1,
            "frames": [[el['frameNum'], 1, 0] + list(piece.pivot)
                       + uv(px_cursor, 0, px_cursor + pw, ph)],
        })
        build["numSymbols"] += 1
        build["numFrames"] += 1
        build["hashes"].append((sdbm(piece.name), piece.name))
        px_cursor += pw + 4
    assert px_cursor <= W, f"atlas overflow: {px_cursor}"

    build["name"] = NEW
    out = ANIM / f"{NEW}_anims" / NEW
    out.mkdir(parents=True, exist_ok=True)

    # --- anim: pedestal bank shows trunk (idle transforms + DELTA) + stand
    n_frames = sum(len(a["frames"]) for a in anim["anims"])
    n_elements = sum(len(f["elements"]) for a in anim["anims"] for f in a["frames"])
    assert anim["h_frames"] == n_frames and anim["h_elements"] == n_elements

    ped = next(a for a in anim["anims"] if a["name"] == "pedestal")
    pla = next(a for a in anim["anims"] if a["name"] == "place")
    pel = pla["frames"][0]["elements"][0]
    prect = pla["frames"][0]["rect"]
    item_hash = item_s["hash"]

    added = 0
    for fr in ped["frames"]:
        fr["rect"] = list(prect)
        base = next(e for e in fr["elements"] if e["symbolHash"] == item_hash)
        base["tx"], base["ty"] = pel["tx"], pel["ty"]
        new_els = []
        for piece in pieces:  # PARTS order = front to back
            src = idle_els[piece.name]
            e = dict(base)
            e["symbolHash"] = sdbm(piece.name)
            if e["folderHash"] == item_hash:
                e["folderHash"] = sdbm(piece.name)
            e["frameNum"] = src["frameNum"]
            for k in ("ma", "mb", "mc", "md"):
                e[k] = src[k]
            e["tx"] = src["tx"] + DELTA[0]
            e["ty"] = src["ty"] + DELTA[1]
            new_els.append(e)
            added += 1
        fr["elements"] = new_els + fr["elements"]
    anim["h_elements"] += added
    anim["maxVisSymbolFrames"] = max(anim["maxVisSymbolFrames"], len(PARTS) + 1)
    for part in PARTS:
        anim["hashes"].append((sdbm(part), part))

    (out / f"{NEW}_build.bytes").write_bytes(write_build(build))
    (out / f"{NEW}_anim.bytes").write_bytes(write_anim(anim))
    atlas.save(out / f"{NEW}_0.png")
    # preview for humans: the composed dummy
    full.save(out.parent / "preview_dummy.png")
    print("wrote", out)


if __name__ == "__main__":
    need = [f"{SRC}_build.bytes", f"{SRC}_anim.bytes", f"{SRC}_0.png",
            f"{BODY}_build.bytes", f"{BODY}_0.png", f"{IDLE}_anim.bytes"]
    if not all((CACHE / n).exists() for n in need):
        extract()
    generate()
