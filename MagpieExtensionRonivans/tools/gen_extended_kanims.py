#!/usr/bin/env python3
"""
Generates hpa_rail_tile_bridge4_kanim / hpa_rail_tile_bridge5_kanim for the
extended Heavy-Duty Joint Plate variants by recomposing the original Ronivans
hpa_rail_tile_bridge kanim — no new art, just element placement.

Layout per W-wide variant (anim origin = footprint center, because
BuildingDef.GetVisualizerOffset() = 0.5 * ((W+1) % 2) puts the visualizer at
the footprint center for even widths and the center cell for odd widths):
  - one tile_fg face element per INTERIOR cell (the gap being covered), drawn
    on top (listed first); the end cells show the truss/cap art instead
  - one full 'outlets' truss composite centered on each END cell; its
    interior-side cap is hidden under the adjacent face (faces are 248px =
    1.24 cells; the cap region sits 0.8..1.28 cells from the composite
    center, inside the neighbor face's span)
  - 'place' frames reuse the exact same element layout as 'on', so the
    placement blueprint silhouette matches the built building (ONI applies
    its own preview tint)
  - 'ui' menu icon unchanged

Cell spacing: 200 sprite-px per grid cell (tile_fg is 248px and covers one
cell with the standard 24px bleed each side; verified against the original
1x1 building where both elements render at identity transform).

Usage: python3 tools/gen_extended_kanims.py
Writes into <repo>/anim/magpie_extended_anims/.
"""
import struct, shutil, copy
from pathlib import Path

SRC = Path.home() / ".config/unity3d/Klei/Oxygen Not Included/mods/Steam/3557584850/anim/HighPressureApplications_anims/hpa_rail_tile_bridge"
REPO = Path(__file__).resolve().parent.parent
OUT_BASE = REPO / "anim" / "magpie_extended_anims"

PX_PER_CELL = 200.0
PLACE_NATIVE_W = 584.0


class R:
    def __init__(self, data): self.d = data; self.o = 0
    def i(self):  v = struct.unpack_from('<i', self.d, self.o)[0]; self.o += 4; return v
    def f(self):  v = struct.unpack_from('<f', self.d, self.o)[0]; self.o += 4; return v
    def s(self):
        n = self.i()
        v = self.d[self.o:self.o+n].decode('utf-8'); self.o += n; return v


class W:
    def __init__(self): self.b = bytearray()
    def i(self, v): self.b += struct.pack('<i', v)
    def f(self, v): self.b += struct.pack('<f', v)
    def s(self, v):
        e = v.encode('utf-8'); self.i(len(e)); self.b += e


def parse_build(data):
    r = R(data)
    assert data[:4] == b'BILD'; r.o = 4
    out = {'version': r.i(), 'numSymbols': r.i(), 'numFrames': r.i(), 'name': r.s()}
    out['symbols'] = []
    for _ in range(out['numSymbols']):
        s = {'hash': r.i(), 'path': r.i(), 'color': r.i(), 'flags': r.i(), 'numFrames': r.i(), 'frames': []}
        for _ in range(s['numFrames']):
            s['frames'].append([r.i(), r.i(), r.i()] + [r.f() for _ in range(8)])
        out['symbols'].append(s)
    out['hashes'] = [(r.i(), r.s()) for _ in range(r.i())]
    assert r.o == len(data)
    return out


def write_build(b):
    w = W(); w.b += b'BILD'
    w.i(b['version']); w.i(b['numSymbols']); w.i(b['numFrames']); w.s(b['name'])
    for s in b['symbols']:
        w.i(s['hash']); w.i(s['path']); w.i(s['color']); w.i(s['flags']); w.i(s['numFrames'])
        for fr in s['frames']:
            for v in fr[:3]: w.i(v)
            for v in fr[3:]: w.f(v)
    w.i(len(b['hashes']))
    for h, name in b['hashes']:
        w.i(h); w.s(name)
    return bytes(w.b)


# Element float layout after the 4 ints: a b g r (color), m_a m_b m_c m_d (2x2
# matrix), m_tx m_ty (translation), order.
EL_INTS = ('symbolHash', 'frameNum', 'folderHash', 'flags')
EL_FLOATS = ('cr', 'cg', 'cb', 'ca', 'ma', 'mb', 'mc', 'md', 'tx', 'ty', 'order')


def parse_anim(data):
    r = R(data)
    assert data[:4] == b'ANIM'; r.o = 4
    out = {'version': r.i(), 'h_elements': r.i(), 'h_frames': r.i(), 'numAnims': r.i()}
    out['anims'] = []
    for _ in range(out['numAnims']):
        a = {'name': r.s(), 'hash': r.i(), 'rate': r.f(), 'numFrames': r.i(), 'frames': []}
        for _ in range(a['numFrames']):
            fr = {'rect': [r.f(), r.f(), r.f(), r.f()], 'elements': []}
            n = r.i()
            for _ in range(n):
                e = {k: r.i() for k in EL_INTS}
                e.update({k: r.f() for k in EL_FLOATS})
                fr['elements'].append(e)
            a['frames'].append(fr)
        out['anims'].append(a)
    out['maxVisSymbolFrames'] = r.i()
    out['hashes'] = [(r.i(), r.s()) for _ in range(r.i())]
    assert r.o == len(data)
    return out


def write_anim(a):
    w = W(); w.b += b'ANIM'
    w.i(a['version']); w.i(a['h_elements']); w.i(a['h_frames']); w.i(a['numAnims'])
    for an in a['anims']:
        w.s(an['name']); w.i(an['hash']); w.f(an['rate']); w.i(an['numFrames'])
        for fr in an['frames']:
            for v in fr['rect']: w.f(v)
            w.i(len(fr['elements']))
            for e in fr['elements']:
                for k in EL_INTS: w.i(e[k])
                for k in EL_FLOATS: w.f(e[k])
    w.i(a['maxVisSymbolFrames'])
    w.i(len(a['hashes']))
    for h, name in a['hashes']:
        w.i(h); w.s(name)
    return bytes(w.b)


def generate(width, build, anim):
    name = f"hpa_rail_tile_bridge{width}"
    # cell offsets relative to the anim origin (footprint center)
    half = (width - 1) / 2.0
    cells = [k - half for k in range(width)]

    nb = copy.deepcopy(build)
    nb['name'] = name

    # Element templates come from the original 'on' anim (face f=0, truss f=1).
    on_src = next(an for an in anim['anims'] if an['name'] == 'on')
    src_els = on_src['frames'][0]['elements']
    face_tpl = next(e for e in src_els if e['folderHash'] == hash_of(anim, 'tile_fg'))
    truss_tpl = next(e for e in src_els if e['folderHash'] == hash_of(anim, 'outlets'))

    def layout(face_frame):
        new = []
        for c in cells[1:-1]:  # faces only on interior cells; first = on top
            e = copy.deepcopy(face_tpl)
            e['frameNum'] = face_frame
            e['tx'] = face_tpl['tx'] + PX_PER_CELL * c
            new.append(e)
        for c in (cells[0], cells[-1]):
            e = copy.deepcopy(truss_tpl)
            e['tx'] = truss_tpl['tx'] + PX_PER_CELL * c
            new.append(e)
        return new

    na = copy.deepcopy(anim)
    total_elements = 0
    for an in na['anims']:
        for fr in an['frames']:
            if an['name'] in ('on', 'off', 'place'):
                # original 'on' uses tile_fg f=0, 'off' f=1; 'place' mirrors 'on'
                fr['elements'] = layout(0 if an['name'] in ('on', 'place') else 1)
            # 'ui' left untouched
            total_elements += len(fr['elements'])
    na['h_frames'] = total_elements  # this header field tracks total elements
    na['maxVisSymbolFrames'] = width

    out = OUT_BASE / name
    out.mkdir(parents=True, exist_ok=True)
    (out / f"{name}_build.bytes").write_bytes(write_build(nb))
    (out / f"{name}_anim.bytes").write_bytes(write_anim(na))
    shutil.copy(SRC / "hpa_rail_tile_bridge_0.png", out / f"{name}_0.png")
    print(f"wrote {out} (elements={total_elements}, cells={cells})")


def hash_of(anim, name):
    for h, n in anim['hashes']:
        if n == name:
            return h
    raise KeyError(name)


def load_validated(src_dir, base_name):
    bd = (src_dir / f"{base_name}_build.bytes").read_bytes()
    ad = (src_dir / f"{base_name}_anim.bytes").read_bytes()
    build = parse_build(bd)
    anim = parse_anim(ad)
    assert write_build(build) == bd, f"build roundtrip failed: {base_name}"
    assert write_anim(anim) == ad, f"anim roundtrip failed: {base_name}"
    png = src_dir / f"{base_name}_0.png"
    if not png.exists():
        png = src_dir / f"{base_name}.png"
    return build, anim, png


def generate_scaled(src_dir, base_name, width, native_width=3):
    """Bridge variants: bake the horizontal stretch (width/native_width) into
    every anim element except 'ui', replacing the old runtime StretchKanim
    transform hack. The anim origin is the footprint center (see
    GetVisualizerOffset note above), which matches the original centered art,
    so translations are unchanged."""
    build, anim, png = load_validated(src_dir, base_name)
    name = f"{base_name}{width}"
    scale = width / float(native_width)

    nb = copy.deepcopy(build)
    nb['name'] = name

    na = copy.deepcopy(anim)
    for an in na['anims']:
        if an['name'] == 'ui':
            continue
        for fr in an['frames']:
            for e in fr['elements']:
                e['ma'] *= scale
                e['mb'] *= scale  # keep any rotation/shear consistent in x
                e['tx'] *= scale

    out = OUT_BASE / name
    out.mkdir(parents=True, exist_ok=True)
    (out / f"{name}_build.bytes").write_bytes(write_build(nb))
    (out / f"{name}_anim.bytes").write_bytes(write_anim(na))
    shutil.copy(png, out / f"{name}_0.png")
    print(f"wrote {out} (scale x{scale:.3f})")


def main():
    bd = (SRC / "hpa_rail_tile_bridge_build.bytes").read_bytes()
    ad = (SRC / "hpa_rail_tile_bridge_anim.bytes").read_bytes()
    build = parse_build(bd)
    anim = parse_anim(ad)
    assert write_build(build) == bd, "build roundtrip failed"
    assert write_anim(anim) == ad, "anim roundtrip failed"
    print("roundtrip OK")
    generate(4, build, anim)
    generate(5, build, anim)

    ronivans_anim = SRC.parent.parent
    for src_rel, base in (("DupesLogistics_anims/logistic_bridge", "logistic_bridge"),
                          ("HighPressureApplications_anims/hpa_rail_bridge", "hpa_rail_bridge"),
                          ("HighPressureApplications_anims/pressure_gas_bridge", "pressure_gas_bridge"),
                          ("HighPressureApplications_anims/pressure_liquid_bridge", "pressure_liquid_bridge")):
        for w in (4, 5):
            generate_scaled(ronivans_anim / src_rel, base, w)


if __name__ == '__main__':
    main()
