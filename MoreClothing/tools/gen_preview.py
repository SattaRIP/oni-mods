#!/usr/bin/env python3
"""Builds the Workshop preview card + a wide showcase image, starring the
Mannequin.

Run with ~/.venvs/oni-kanim/bin/python AFTER the other gen_* tools (it
crops item sprites from the generated builds and dresses statues with the
worn builds). Writes:

  MoreClothing/preview2.png            512x512 Workshop card
  MoreClothing/screenshots/mannequins.png   1200x630 showcase

Statues are rendered with the exact piece pipeline the building uses
(idle-pose elements from gen_mannequin_kanim, worn-art substitution, drawn
boots from gen_worn_feet_kanims, real hat accessory art from
hat_swap_build).
"""
import sys
from pathlib import Path

TOOLS = Path(__file__).resolve().parent
sys.path.insert(0, str(TOOLS))
import gen_mannequin_kanim as gm
import gen_worn_feet_kanims as gw
from gen_extended_kanims import parse_build, parse_anim

from PIL import Image, ImageDraw, ImageFont, ImageFilter

REPO = TOOLS.parent
CACHE = REPO / "tools" / "vanilla_kanim_cache"
FONT = "/usr/share/fonts/TTF/DejaVuSans-Bold.ttf"


# ---------------------------------------------------------------- sprites
def sprite(name):
    d = REPO / f"anim/{name}_anims/{name}"
    b = parse_build((d / f"{name}_build.bytes").read_bytes())
    table = dict(b["hashes"])
    img = Image.open(d / f"{name}_0.png")
    W, H = img.size
    for s in b["symbols"]:
        if table.get(s["hash"]) == "object":
            f = s["frames"][0]
            u0, v0, u1, v1 = f[-4:]
            return img.crop((int(u0 * W), int(v0 * H), int(u1 * W), int(v1 * H)))
    sys.exit(f"{name}: no object symbol")


def up(im, s):
    return im.resize((int(im.width * s), int(im.height * s)), Image.LANCZOS)


def fit(im, w, h):
    s = min(w / im.width, h / im.height)
    return up(im, s)


# ---------------------------------------------------------------- statues
def load_build(bytes_path, png_path):
    b = parse_build(Path(bytes_path).read_bytes())
    img = Image.open(png_path).convert("RGBA")
    table = {dict(b["hashes"]).get(s["hash"], s["hash"]): s for s in b["symbols"]}
    return b, img, table


def crop_frame(img, fr):
    W, H = img.size
    u0, v0, u1, v1 = fr[7:]
    for y0, y1 in ((int(v0 * H), int(v1 * H)),
                   (int((1 - v1) * H), int((1 - v0) * H))):
        c = img.crop((int(u0 * W), y0, int(u1 * W), y1))
        if c.getbbox() is not None:
            return c
    return img.crop((int(u0 * W), int(v0 * H), int(u1 * W), int(v1 * H)))


def frame_for(sym, fnum):
    return next(f for f in sym["frames"] if f[0] <= fnum < f[0] + f[1])


def build_pieces(garment=None, boots=False, hat=None):
    body = parse_build((CACHE / f"{gm.BODY}_build.bytes").read_bytes())
    body_img = Image.open(CACHE / f"{gm.BODY}_0.png").convert("RGBA")
    body_table = dict(body["hashes"])
    body_sym = {body_table.get(s["hash"]): s for s in body["symbols"]}

    idle = parse_anim((CACHE / f"{gm.IDLE}_anim.bytes").read_bytes())
    idle_table = dict(idle["hashes"])
    bank = next(a for a in idle["anims"] if a["name"] == "idle_default")
    idle_elements = [e for e in bank["frames"][0]["elements"]
                     if idle_table.get(e["symbolHash"]) in gm.PARTS]

    g_img, g_table = None, {}
    if garment:
        d = REPO / "anim" / f"{garment}_anims" / garment
        _, g_img, g_table = load_build(d / f"{garment}_build.bytes",
                                       d / f"{garment}_0.png")
    hat_img, hat_table = None, {}
    if hat:
        _, hat_img, hat_table = load_build(CACHE / "hat_swap_build.bytes",
                                           CACHE / "hat_swap_0.png")

    pieces = []
    for e in idle_elements:
        part = idle_table[e["symbolHash"]]
        fnum = e["frameNum"]
        if part == "snapto_headshape":
            art = gm.draw_head(int(gm.HEAD_PIVOT[2]), int(gm.HEAD_PIVOT[3]))
            pivot = gm.HEAD_PIVOT
        elif part == "snapto_hat":
            if not hat:
                continue
            fr = frame_for(hat_table[hat], fnum)
            art, pivot = crop_frame(hat_img, fr), tuple(fr[3:7])
        elif part == "foot" and boots:
            art, pivot = gw.draw_boot(), gw.BOOT_PIVOT
        elif garment and part in g_table:
            fr = frame_for(g_table[part], fnum)
            art, pivot = crop_frame(g_img, fr), tuple(fr[3:7])
        else:
            fr = frame_for(body_sym[part], fnum)
            art = gm.linenize(crop_frame(body_img, fr))
            pivot = tuple(fr[3:7])
        pieces.append(gm.Piece(part, art, pivot, e))
    return pieces


def render_statue(pieces, scale=2):
    W, H = 288 * scale, 480 * scale
    canvas = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(canvas)
    d._img = canvas
    gm.draw_stand(d, gm.world_to_box(W, H))
    gm.composite_trunk(canvas, pieces, gm.world_to_box(W, H))
    return canvas.crop(canvas.getbbox())


# ---------------------------------------------------------------- layout
def gradient(w, h):
    im = Image.new("RGBA", (w, h))
    d = ImageDraw.Draw(im)
    for y in range(h):
        t = y / (h - 1)
        d.line([(0, y), (w, y)],
               fill=(int(24 + 20 * t), int(24 + 50 * t), int(46 + 60 * t), 255))
    return im


def paste_shadowed(card, im, pos):
    sh = Image.new("RGBA", im.size, (0, 0, 0, 0))
    sh.paste((0, 0, 0, 130), mask=im.split()[3])
    card.alpha_composite(sh.filter(ImageFilter.GaussianBlur(5)),
                         (pos[0] + 7, pos[1] + 9))
    card.alpha_composite(im, pos)


def center(d, w, y, text, font, fill):
    tw = d.textlength(text, font=font)
    x = (w - tw) / 2
    d.text((x + 3, y + 3), text, font=font, fill=(0, 0, 0, 180))
    d.text((x, y), text, font=font, fill=fill)


def main():
    _, _, hat_table = load_build(CACHE / "hat_swap_build.bytes",
                                 CACHE / "hat_swap_0.png")

    def pick(*prefixes):
        for p in prefixes:
            for n in sorted(map(str, hat_table)):
                if n.startswith(p):
                    return n
        return sorted(map(str, hat_table))[0]

    hat_a = pick("hat_role_mining")
    hat_b = pick("hat_role_cooking", "hat_role_kitchen", "hat_role_basekeeping")
    hat_c = pick("hat_role_astronaut")
    print("hats:", hat_a, hat_b, hat_c)

    swim = render_statue(build_pieces("body_snazzy_swimwear", boots=True, hat=hat_a))
    coat = render_statue(build_pieces("body_upgraded_warm_coat", hat=hat_b))
    naked = render_statue(build_pieces(hat=hat_c))

    # ------------------------------------------------ 512x512 card
    card = gradient(512, 512)
    d = ImageDraw.Draw(card)

    a = fit(swim, 175, 295)
    b = fit(coat, 175, 295)
    paste_shadowed(card, a, (256 - a.width - 12, 165))
    paste_shadowed(card, b, (268, 165))

    for name, scale, pos in (("upgraded_warm_coat_item", 1.9, (6, 352)),
                             ("snazzy_swimwear_item", 1.9, (388, 352)),
                             ("snazzy_rubber_boots_item", 1.6, (10, 190)),
                             ("snazzy_shoes_item", 1.6, (400, 190))):
        paste_shadowed(card, up(sprite(name), scale), pos)

    f_big = ImageFont.truetype(FONT, 58)
    f_sub = ImageFont.truetype(FONT, 20)
    center(d, 512, 26, "MORE", f_big, (240, 244, 250, 255))
    center(d, 512, 88, "CLOTHING", f_big, (240, 244, 250, 255))
    center(d, 512, 472, "coats · suits · snazzy things · mannequins",
           f_sub, (255, 195, 90, 255))
    card.convert("RGB").save(REPO / "preview2.png")
    print("wrote", REPO / "preview2.png")

    # ------------------------------------------------ wide showcase
    shot = gradient(1200, 630)
    d = ImageDraw.Draw(shot)
    f_head = ImageFont.truetype(FONT, 46)
    f_cap = ImageFont.truetype(FONT, 24)
    center(d, 1200, 34, "Mannequins that wear your clothes",
           f_head, (240, 244, 250, 255))
    slots = ((swim, 200, "garments · boots · hats"),
             (naked, 600, "dupe-shaped, dupe-sized"),
             (coat, 1000, "decor that impresses"))
    for im, cx, cap in slots:
        s = fit(im, 260, 430)
        paste_shadowed(shot, s, (cx - s.width // 2, 585 - s.height - 45))
        tw = d.textlength(cap, font=f_cap)
        d.text((cx - tw / 2 + 2, 552 + 2), cap, font=f_cap, fill=(0, 0, 0, 180))
        d.text((cx - tw / 2, 552), cap, font=f_cap, fill=(255, 195, 90, 255))
    out = REPO / "screenshots"
    out.mkdir(exist_ok=True)
    shot.convert("RGB").save(out / "mannequins.png")
    print("wrote", out / "mannequins.png")


if __name__ == "__main__":
    main()
