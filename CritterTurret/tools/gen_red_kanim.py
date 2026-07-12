#!/usr/bin/env python3
"""
Builds the turret's own red kanim ("critterturret_kanim") from the vanilla
Auto-Miner art, replacing the runtime TintColour hack — which only coloured the
placed building and left the build-menu icon / drag preview orange.

Extracts auto_miner_{anim,build}.bytes + atlas from the game's Unity assets
(UnityPy; run with ~/.venvs/oni-kanim/bin/python), renames the build to
"critterturret" (Magpie parser/writer, roundtrip-validated), and multiplies the
atlas RGB by TURRET_RED (1.0, 0.30, 0.28) so the art matches what TintColour
produced in-game. Output goes to anim/critter_turret_anims/critterturret/,
which build.sh copies into dist/.

Re-run after game updates if Klei changes the Auto-Miner art.
"""
import sys
from pathlib import Path

REPO = Path(__file__).resolve().parent.parent
sys.path.insert(0, str(REPO.parent / "MagpieExtensionRonivans" / "tools"))
from gen_extended_kanims import parse_build, write_build, parse_anim, write_anim

GAME_DATA = Path.home() / ".local/share/Steam/steamapps/common/OxygenNotIncluded/OxygenNotIncluded_Data"
CACHE = REPO / "tools" / "vanilla_kanim_cache" / "auto_miner"
OUT = REPO / "anim" / "critter_turret_anims" / "critterturret"
TURRET_RED = (1.0, 0.22, 0.18)  # the brain's old TURRET_RED tint (it overrode the config's weaker one)


def extract():
    import UnityPy
    CACHE.mkdir(parents=True, exist_ok=True)
    wanted_text = {"auto_miner_anim", "auto_miner_build"}
    found = {}
    for assets in sorted(GAME_DATA.glob("*.assets")):
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
            elif obj.type.name == "Texture2D" and name.startswith("auto_miner_") \
                    and name.removeprefix("auto_miner_").isdigit():
                data.image.save(CACHE / f"{name}.png")
                found[name] = assets.name
    print("extracted:", found)
    missing = wanted_text | {"auto_miner_0"}
    missing -= found.keys()
    if missing:
        sys.exit(f"MISSING from game assets: {missing}")


def generate():
    from PIL import Image
    bd = (CACHE / "auto_miner_build.bytes").read_bytes()
    ad = (CACHE / "auto_miner_anim.bytes").read_bytes()
    build, anim = parse_build(bd), parse_anim(ad)
    assert write_build(build) == bd, "build roundtrip failed"
    assert write_anim(anim) == ad, "anim roundtrip failed"

    build["name"] = "critterturret"
    OUT.mkdir(parents=True, exist_ok=True)
    (OUT / "critterturret_build.bytes").write_bytes(write_build(build))
    (OUT / "critterturret_anim.bytes").write_bytes(write_anim(anim))

    for src in sorted(CACHE.glob("auto_miner_*.png")):
        img = Image.open(src).convert("RGBA")
        r, g, b, a = img.split()
        r = r.point(lambda v: round(v * TURRET_RED[0]))
        g = g.point(lambda v: round(v * TURRET_RED[1]))
        b = b.point(lambda v: round(v * TURRET_RED[2]))
        idx = src.stem.removeprefix("auto_miner_")
        Image.merge("RGBA", (r, g, b, a)).save(OUT / f"critterturret_{idx}.png")
    print("wrote", OUT)


if __name__ == "__main__":
    if not (CACHE / "auto_miner_build.bytes").exists():
        extract()
    generate()
