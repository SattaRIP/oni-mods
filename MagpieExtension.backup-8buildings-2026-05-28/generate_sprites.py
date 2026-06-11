#!/usr/bin/env python3
"""
Generates 2-tile and 3-tile bridge sprites from 1-tile source sprites.

Each bridge sprite is a horizontal strip. We extend the middle section
(between the left and right connection points) to make it wider.

Usage:
    python3 generate_sprites.py

Requires: Pillow  (pip install Pillow)

Sources extracted from:
  - Ronivans Legacy: pressure_gas_bridge.png, pressure_liquid_bridge.png
  - Vanilla ONI anim (needs to be extracted separately): logicwire, logicribbon
"""

import shutil
import struct
import os
from pathlib import Path
from PIL import Image

# ---------------------------------------------------------------------------
# Paths
# ---------------------------------------------------------------------------
RONIVANS_BIN = Path.home() / ".steam/steam/steamapps/workshop/content/457140/3557584850"
MAGPIE_BIN   = Path.home() / ".steam/steam/steamapps/workshop/content/457140/2861126557"
ONI_MANAGED  = Path.home() / ".steam/steam/steamapps/common/OxygenNotIncluded/OxygenNotIncluded_Data/Managed"

OUT_BASE = Path(__file__).parent / "anim/assets"

# ---------------------------------------------------------------------------
# Sprite definitions: (name, source_zip, path_in_zip, anim_bytes_path)
# ---------------------------------------------------------------------------
SOURCES = [
    {
        "name":       "hpgas",
        "bin":        RONIVANS_BIN,
        "png_path":   "anim\\HighPressureApplications_anims\\pressure_gas_bridge\\pressure_gas_bridge.png",
        "anim_path":  "anim\\HighPressureApplications_anims\\pressure_gas_bridge\\pressure_gas_bridge_anim.bytes",
        "build_path": "anim\\HighPressureApplications_anims\\pressure_gas_bridge\\pressure_gas_bridge_build.bytes",
        "anim_name":  "pressure_gas_bridge",
    },
    {
        "name":       "hpliquid",
        "bin":        RONIVANS_BIN,
        "png_path":   "anim\\HighPressureApplications_anims\\pressure_liquid_bridge\\pressure_liquid_bridge.png",
        "anim_path":  "anim\\HighPressureApplications_anims\\pressure_liquid_bridge\\pressure_liquid_bridge_anim.bytes",
        "build_path": "anim\\HighPressureApplications_anims\\pressure_liquid_bridge\\pressure_liquid_bridge_build.bytes",
        "anim_name":  "pressure_liquid_bridge",
    },
    # For logic wire / ribbon, extract from the vanilla game's StreamingAssets kanim
    # These will be handled separately below via the vanilla game path
]

CELL_PX = 64   # ONI grid cell size in pixels

def extract_from_zip(bin_folder: Path, inner_path: str) -> bytes:
    """Extract a file from the first .bin (zip) in bin_folder."""
    import zipfile
    bin_files = list(bin_folder.glob("*.bin"))
    if not bin_files:
        raise FileNotFoundError(f"No .bin file found in {bin_folder}")
    with zipfile.ZipFile(bin_files[0]) as zf:
        return zf.read(inner_path)


def stretch_bridge_png(src_img: Image.Image, extra_cells: int) -> Image.Image:
    """
    Extend a bridge sprite horizontally by duplicating the middle column(s).

    A 1-tile bridge is 3 cells wide (left end + span + right end).
    We add `extra_cells` cells in the middle.

    Strategy: take the middle third of the image and tile it to fill the gap.
    """
    w, h = src_img.size
    cell_w = w // 3                # width of one cell section
    left   = src_img.crop((0,      0, cell_w,     h))
    mid    = src_img.crop((cell_w, 0, cell_w * 2, h))
    right  = src_img.crop((cell_w * 2, 0, w,      h))

    new_w = w + extra_cells * cell_w
    out = Image.new("RGBA", (new_w, h), (0, 0, 0, 0))
    out.paste(left, (0, 0))
    for i in range(1 + extra_cells):
        out.paste(mid, (cell_w + i * cell_w, 0))
    out.paste(right, (new_w - cell_w, 0))
    return out


def copy_bytes_files(src_anim: bytes, src_build: bytes, dest_dir: Path, anim_name: str):
    """
    Copy the .bytes files as-is. The anim/build format references the PNG by
    the animation bank name, not by pixel coordinates, so they work for any
    width as long as the PNG file name stays the same.
    """
    dest_dir.mkdir(parents=True, exist_ok=True)
    (dest_dir / f"{anim_name}_anim.bytes").write_bytes(src_anim)
    (dest_dir / f"{anim_name}_build.bytes").write_bytes(src_build)


def generate_pair(source: dict):
    """Generate 2-tile and 3-tile versions of a bridge sprite."""
    print(f"\n=== Generating sprites for {source['name']} ===")

    png_bytes   = extract_from_zip(source["bin"], source["png_path"])
    anim_bytes  = extract_from_zip(source["bin"], source["anim_path"])
    build_bytes = extract_from_zip(source["bin"], source["build_path"])

    src_img = Image.open(__import__("io").BytesIO(png_bytes)).convert("RGBA")
    print(f"  Source image: {src_img.size[0]}×{src_img.size[1]}px")

    anim_name = source["anim_name"]

    for extra, suffix in [(1, "2"), (2, "3")]:
        stretched = stretch_bridge_png(src_img, extra)
        dest_dir = OUT_BASE / f"{source['name']}_bridge{suffix}"
        dest_dir.mkdir(parents=True, exist_ok=True)

        out_png = dest_dir / f"{anim_name}.png"
        stretched.save(out_png, "PNG")
        print(f"  [{suffix}-tile] {out_png}  ({stretched.size[0]}×{stretched.size[1]}px)")

        copy_bytes_files(anim_bytes, build_bytes, dest_dir, anim_name)


def generate_logic_bridges():
    """
    Logic wire and ribbon bridge sprites live inside the game's StreamingAssets.
    We look for them there; if unavailable we copy from Magpie's wire bridge as
    a visual-placeholder fallback (automation wire colour is different but it works).
    """
    ONI_STREAM = Path.home() / ".steam/steam/steamapps/common/OxygenNotIncluded/OxygenNotIncluded_Data/StreamingAssets/anim/assets"

    # Vanilla kanim names for logic bridges
    logic_sources = [
        {
            "name":      "logicwire",
            "src_dir":   ONI_STREAM / "logicwirebridge",
            "anim_name": "logicwirebridge",
            "fallback_dir": None,  # Will try Magpie wire bridge as fallback
        },
        {
            "name":      "logicribbon",
            "src_dir":   ONI_STREAM / "logicribbonbridge",
            "anim_name": "logicribbonbridge",
            "fallback_dir": None,
        },
    ]

    for src in logic_sources:
        png_file = src["src_dir"] / f"{src['anim_name']}.png"
        if not png_file.exists():
            # Try alternative casing
            candidates = list(src["src_dir"].glob("*.png")) if src["src_dir"].exists() else []
            if candidates:
                png_file = candidates[0]
            else:
                print(f"  WARNING: Could not find {png_file}. "
                      f"Using Magpie wire bridge as visual placeholder.")
                # Use Magpie's 2-tile wire bridge sprite as placeholder
                _use_magpie_fallback(src["name"])
                continue

        anim_file  = png_file.with_suffix("").with_name(png_file.stem + "_anim.bytes")
        build_file = png_file.with_suffix("").with_name(png_file.stem + "_build.bytes")

        src_img    = Image.open(png_file).convert("RGBA")
        anim_bytes = anim_file.read_bytes()  if anim_file.exists()  else b""
        build_bytes= build_file.read_bytes() if build_file.exists() else b""

        anim_name = src["anim_name"]
        for extra, suffix in [(1, "2"), (2, "3")]:
            stretched = stretch_bridge_png(src_img, extra)
            dest_dir  = OUT_BASE / f"{src['name']}_bridge{suffix}"
            dest_dir.mkdir(parents=True, exist_ok=True)

            stretched.save(dest_dir / f"{anim_name}.png", "PNG")
            if anim_bytes:
                (dest_dir / f"{anim_name}_anim.bytes").write_bytes(anim_bytes)
            if build_bytes:
                (dest_dir / f"{anim_name}_build.bytes").write_bytes(build_bytes)

        print(f"  Generated {src['name']} bridge 2/3 sprites from {png_file.name}")


def _use_magpie_fallback(logic_type: str):
    """Copy Magpie's wire bridge sprites as a visual placeholder for logic bridges."""
    import zipfile, io
    bin_files = list(MAGPIE_BIN.glob("*.bin"))
    if not bin_files:
        print("  Cannot find Magpie Bridge mod zip. Skipping placeholder.")
        return

    anim_name_map = {
        "logicwire":   ("dianxianqiao2", "utilityElectricBridge"),
        "logicribbon": ("dianxianqiao2", "utilityElectricBridge"),
    }
    folder, anim_name = anim_name_map[logic_type]

    with zipfile.ZipFile(bin_files[0]) as zf:
        try:
            png_bytes   = zf.read(f"anim\\assets\\{folder}\\{anim_name}.png")
            anim_bytes  = zf.read(f"anim\\assets\\{folder}\\{anim_name}_anim.bytes")
            build_bytes = zf.read(f"anim\\assets\\{folder}\\{anim_name}_build.bytes")
        except KeyError:
            print(f"  Magpie fallback file not found for {logic_type}")
            return

    src_img = Image.open(io.BytesIO(png_bytes)).convert("RGBA")
    out_anim_name = f"{logic_type}bridge"

    for extra, suffix in [(1, "2"), (2, "3")]:
        stretched = stretch_bridge_png(src_img, extra)
        dest_dir  = OUT_BASE / f"{logic_type}_bridge{suffix}"
        dest_dir.mkdir(parents=True, exist_ok=True)

        stretched.save(dest_dir / f"{out_anim_name}.png", "PNG")
        (dest_dir / f"{out_anim_name}_anim.bytes").write_bytes(anim_bytes)
        (dest_dir / f"{out_anim_name}_build.bytes").write_bytes(build_bytes)

    print(f"  Used Magpie placeholder for {logic_type}")


if __name__ == "__main__":
    try:
        from PIL import Image
    except ImportError:
        print("ERROR: Pillow not installed. Run: pip install Pillow")
        raise SystemExit(1)

    OUT_BASE.mkdir(parents=True, exist_ok=True)

    for source in SOURCES:
        generate_pair(source)

    generate_logic_bridges()

    print("\nDone! Sprite assets written to:", OUT_BASE)
