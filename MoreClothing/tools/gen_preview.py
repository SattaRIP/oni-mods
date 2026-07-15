#!/usr/bin/env python3
"""Builds the 512x512 Workshop preview card from the mod's own item kanims.

Run with ~/.venvs/oni-kanim/bin/python AFTER gen_*_kanims.py (it crops the
item sprites out of the generated builds). Writes MoreClothing/preview.png.
"""
import sys
from pathlib import Path

REPO = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(REPO.parent / "MagpieExtensionRonivans" / "tools"))
from gen_extended_kanims import parse_build

from PIL import Image, ImageDraw, ImageFont, ImageFilter

FONT = "/usr/share/fonts/TTF/DejaVuSans-Bold.ttf"


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


def main():
    card = Image.new("RGBA", (512, 512))
    d = ImageDraw.Draw(card)
    for y in range(512):
        t = y / 511
        d.line([(0, y), (512, y)],
               fill=(int(24 + 20 * t), int(24 + 50 * t), int(46 + 60 * t), 255))

    items = [
        (up(sprite("upgraded_warm_coat_item"), 2.6), (18, 150)),
        (up(sprite("eva_suit_item"), 2.6), (270, 145)),
        (up(sprite("snazzy_swimwear_item"), 2.6), (20, 300)),
        (up(sprite("snazzy_rubber_boots_item"), 2.4), (210, 320)),
        (up(sprite("snazzy_shoes_item"), 2.4), (360, 305)),
    ]
    for im, pos in items:
        sh = Image.new("RGBA", im.size, (0, 0, 0, 0))
        sh.paste((0, 0, 0, 130), mask=im.split()[3])
        card.alpha_composite(sh.filter(ImageFilter.GaussianBlur(5)),
                             (pos[0] + 7, pos[1] + 9))
        card.alpha_composite(im, pos)

    f_big = ImageFont.truetype(FONT, 58)
    f_sub = ImageFont.truetype(FONT, 22)

    def center(y, text, font, fill):
        w = d.textlength(text, font=font)
        x = (512 - w) / 2
        d.text((x + 3, y + 3), text, font=font, fill=(0, 0, 0, 180))
        d.text((x, y), text, font=font, fill=fill)

    center(30, "MORE", f_big, (240, 244, 250, 255))
    center(92, "CLOTHING", f_big, (240, 244, 250, 255))
    center(468, "coats · suits · snazzy things", f_sub, (255, 195, 90, 255))
    card.convert("RGB").save(REPO / "preview.png")
    print("wrote", REPO / "preview.png")


if __name__ == "__main__":
    main()
