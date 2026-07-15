import sys
from pathlib import Path
import UnityPy

GAME_DATA = Path.home() / ".local/share/Steam/steamapps/common/OxygenNotIncluded/OxygenNotIncluded_Data"
OUT = Path(__file__).parent / "swimwear_cache"
OUT.mkdir(exist_ok=True)

want_prefix = ("wetsuit_item_", "rubber_boots_item_", "body_wetsuit_")
want_text = {"wetsuit_item_anim", "wetsuit_item_build",
             "rubber_boots_item_anim", "rubber_boots_item_build",
             "body_wetsuit_anim", "body_wetsuit_build"}
found = {}
targets = sorted(GAME_DATA.glob("*.assets"))
targets += sorted((GAME_DATA / "StreamingAssets").glob("*bundle*"))
for assets in targets:
    env = UnityPy.load(str(assets))
    for obj in env.objects:
        if obj.type.name not in ("TextAsset", "Texture2D"):
            continue
        data = obj.read()
        name = data.m_Name
        if obj.type.name == "TextAsset" and name in want_text:
            raw = data.m_Script
            if isinstance(raw, str):
                raw = raw.encode("utf-8", "surrogateescape")
            (OUT / f"{name}.bytes").write_bytes(raw)
            found[name] = assets.name
        elif obj.type.name == "Texture2D":
            for p in want_prefix:
                if name.startswith(p) and name[len(p):].isdigit():
                    data.image.save(OUT / f"{name}.png")
                    found[name] = assets.name
print("extracted:", sorted(found))
