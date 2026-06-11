#!/usr/bin/env bash
# Build script for MagpieExtensionRonivans ONI mod.
# Ronivans Legacy must be installed at runtime; we use reflection so no compile-time link is needed.
set -e

ONI_MANAGED="$HOME/.steam/steam/steamapps/common/OxygenNotIncluded/OxygenNotIncluded_Data/Managed"

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

echo "==> Compiling C#..."
mkdir -p "$SCRIPT_DIR/dist"

mcs \
  -target:library \
  -out:"$SCRIPT_DIR/dist/MagpieExtensionRonivans.dll" \
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
cp "$SCRIPT_DIR/mod.yaml" "$SCRIPT_DIR/dist/"
cp "$SCRIPT_DIR/mod_info.yaml" "$SCRIPT_DIR/dist/"
# Generated wide joint-plate kanims (tools/gen_extended_kanims.py)
rm -rf "$SCRIPT_DIR/dist/anim"
cp -r "$SCRIPT_DIR/anim" "$SCRIPT_DIR/dist/anim"

echo ""
echo "=== Build complete! Files in: $SCRIPT_DIR/dist/ ==="
echo ""
echo "To install:"
echo "  mkdir -p ~/.config/unity3d/Klei/Oxygen\\ Not\\ Included/mods/Local/MagpieExtensionRonivans"
echo "  cp -r $SCRIPT_DIR/dist/* ~/.config/unity3d/Klei/Oxygen\\ Not\\ Included/mods/Local/MagpieExtensionRonivans/"
