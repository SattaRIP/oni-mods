#!/usr/bin/env bash
# Build the Protective Wear ONI mod: recolour kanims, compile C#, package, deploy to Dev.
set -e

ONI_MANAGED="$HOME/.local/share/Steam/steamapps/common/OxygenNotIncluded/OxygenNotIncluded_Data/Managed"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
DEV="$HOME/.config/unity3d/Klei/Oxygen Not Included/mods/Dev/ProtectiveWear"
VENV_PY="$HOME/.venvs/oni-kanim/bin/python"

echo "==> Generating recoloured kanims..."
"$VENV_PY" "$SCRIPT_DIR/tools/gen_protective_kanims.py"

echo "==> Compiling C#..."
mkdir -p "$SCRIPT_DIR/dist"
mcs \
  -target:library \
  -out:"$SCRIPT_DIR/dist/ProtectiveWear.dll" \
  -optimize+ \
  -langversion:7.2 \
  -r:"$ONI_MANAGED/Assembly-CSharp.dll" \
  -r:"$ONI_MANAGED/Assembly-CSharp-firstpass.dll" \
  -r:"$ONI_MANAGED/UnityEngine.CoreModule.dll" \
  -r:"$ONI_MANAGED/UnityEngine.dll" \
  -r:"$ONI_MANAGED/0Harmony.dll" \
  -r:"$ONI_MANAGED/netstandard.dll" \
  "$SCRIPT_DIR/src/"*.cs

echo "==> Packaging mod..."
cp "$SCRIPT_DIR/mod.yaml"      "$SCRIPT_DIR/dist/"
cp "$SCRIPT_DIR/mod_info.yaml" "$SCRIPT_DIR/dist/"
[ -f "$SCRIPT_DIR/preview.png" ] && cp "$SCRIPT_DIR/preview.png" "$SCRIPT_DIR/dist/"
rm -rf "$SCRIPT_DIR/dist/anim"
cp -r "$SCRIPT_DIR/anim"       "$SCRIPT_DIR/dist/"

echo "==> Deploying to Dev/ProtectiveWear ..."
rm -rf "$DEV"
mkdir -p "$DEV"
cp -r "$SCRIPT_DIR/dist/." "$DEV/"

echo ""
echo "=== Build + deploy complete. Dev mod at: $DEV ==="
ls -1 "$DEV"
