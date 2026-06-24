#!/usr/bin/env python3
"""Widen single-symbol wire bridge kanims (round terminals + middle wire) by
keeping the end-terminal regions at native size and stretching ONLY the middle
wire band, then bumping the symbol's logical width. No runtime anim scaling, so
the round terminals never distort. Generates <base>4 / <base>5."""
import sys, copy
from pathlib import Path
from PIL import Image
sys.path.insert(0,"/home/mythraps/Documents/ONI_Mods/MagpieExtensionRonivans/tools")
import gen_extended_kanims as g

REPO=Path(__file__).resolve().parent.parent
CACHE=REPO/"tools"/"vanilla_kanim_cache"
OUT=REPO/"anim"/"magpie_extended_anims"
PX_PER_CELL=200.0           # logical units / cell
ATLAS_PER_LOGICAL=0.5       # atlas px / logical unit (art stored at half res)
NATIVE_CELLS=3
# middle band of the atlas-space bridge image to stretch (terminals lie outside)
MID_L_FRAC=0.40
MID_R_FRAC=0.60

def widen_image(im, ext_px):
    w,h=im.size
    L=int(w*MID_L_FRAC); R=int(w*MID_R_FRAC)
    left=im.crop((0,0,L,h)); mid=im.crop((L,0,R,h)); right=im.crop((R,0,w,h))
    mid=mid.resize((max(1,(R-L)+ext_px),h), Image.LANCZOS)
    out=Image.new("RGBA",(w+ext_px,h),(0,0,0,0))
    out.paste(left,(0,0)); out.paste(mid,(L,0)); out.paste(right,(L+mid.size[0],0))
    return out

def generate(base, width):
    build,anim,_=g.load_validated(CACHE/base, base)
    atlas=Image.open(CACHE/base/f"{base}_0.png").convert("RGBA"); AW,AH=atlas.size
    ext_logical=(width-NATIVE_CELLS)*PX_PER_CELL
    ext_px=int(round(ext_logical*ATLAS_PER_LOGICAL))
    bn={h:n for h,n in build['hashes']}
    # extract + (maybe) widen each symbol image
    sym_imgs={}; new_pivotW={}
    for s in build['symbols']:
        name=bn.get(s['hash'],'?'); fr=s['frames'][0]; fl=fr[3:]
        pivX,pivY,pivW,pivH,x1,y1,x2,y2=fl
        rect=(int(round(x1*AW)),int(round(y1*AH)),int(round(x2*AW)),int(round(y2*AH)))
        im=atlas.crop(rect)
        if name in ('bridge','place'):
            im=widen_image(im,ext_px); new_pivotW[name]=pivW+ext_logical
        else:
            new_pivotW[name]=pivW
        sym_imgs[name]=im
    # pack into a new atlas (vertical stack, padded to max width)
    order=[bn.get(s['hash'],'?') for s in build['symbols']]
    NW=max(im.size[0] for im in sym_imgs.values())
    NH=sum(im.size[1] for im in sym_imgs.values())
    def _pot(n):
        p=1
        while p<n: p*=2
        return p
    PW,PH=_pot(NW),_pot(NH)   # power-of-2 atlas (ONI/Unity-safe)
    newatlas=Image.new("RGBA",(PW,PH),(0,0,0,0)); ypos={}; y=0
    for name in order:
        im=sym_imgs[name]; newatlas.paste(im,(0,y)); ypos[name]=(0,y,im.size[0],im.size[1]); y+=im.size[1]
    # rewrite build: pivotW + UV per symbol (UVs normalized to PoT atlas)
    nb=copy.deepcopy(build); nb['name']=f"{base}{width}"
    for s in nb['symbols']:
        name=bn.get(s['hash'],'?'); fr=s['frames'][0]
        px,py,pw,ph=ypos[name]
        fr[3+2]=new_pivotW[name]                 # pivotW (index 2 of floats)
        fr[3+4]=px/PW; fr[3+5]=py/PH             # x1,y1
        fr[3+6]=(px+pw)/PW; fr[3+7]=(py+ph)/PH   # x2,y2
    # anim unchanged (ma stays 1 -> renders at new pivotW, no terminal distortion)
    na=copy.deepcopy(anim); na['name']=f"{base}{width}"
    out=OUT/f"{base}{width}"; out.mkdir(parents=True,exist_ok=True)
    (out/f"{base}{width}_build.bytes").write_bytes(g.write_build(nb))
    (out/f"{base}{width}_anim.bytes").write_bytes(g.write_anim(na))
    newatlas.save(out/f"{base}{width}_0.png")
    print(f"wrote {out.name}: atlas {NW}x{NH}, bridge pivotW {new_pivotW.get('bridge')}, ext_px {ext_px}")
    return out

if __name__=='__main__':
    bases=sys.argv[1:] or ["utilityelectricbridge","utilityelectricbridgeconductive","utilityelectricbridgerubber"]
    for b in bases:
        for w in (4,5): generate(b,w)
