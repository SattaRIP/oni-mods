from pathlib import Path
import UnityPy
GAME_DATA = Path.home() / ".local/share/Steam/steamapps/common/OxygenNotIncluded/OxygenNotIncluded_Data"
OUT = Path("swimwear_cache"); OUT.mkdir(exist_ok=True)
want = ("shirt_decor01_", "body_shirt_decor01_")
found = {}
targets = sorted(GAME_DATA.glob("*.assets")) + sorted((GAME_DATA / "StreamingAssets").glob("*bundle*"))
for assets in targets:
    env = UnityPy.load(str(assets))
    for obj in env.objects:
        if obj.type.name != "Texture2D": continue
        data = obj.read(); name = data.m_Name
        for p in want:
            if name.startswith(p) and name[len(p):].isdigit():
                data.image.save(OUT / f"{name}.png"); found[name] = assets.name
print("extracted:", sorted(found))
