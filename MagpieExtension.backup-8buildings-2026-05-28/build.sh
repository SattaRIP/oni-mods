#!/usr/bin/env bash
# Build script for MagpieExtension ONI mod (uses Mono mcs compiler)
set -e

ONI_MANAGED="$HOME/.steam/steam/steamapps/common/OxygenNotIncluded/OxygenNotIncluded_Data/Managed"
MAGPIE_BIN="$HOME/.steam/steam/steamapps/workshop/content/457140/2861126557"
RONIVANS_BIN="$HOME/.steam/steam/steamapps/workshop/content/457140/3557584850"

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
TMPLIBS="$(mktemp -d)"
trap "rm -rf $TMPLIBS" EXIT

echo "==> Extracting dependency DLLs..."
unzip -q "$(ls "$MAGPIE_BIN"/*.bin | head -1)"   "PLib.dll"                           -d "$TMPLIBS"
unzip -q "$(ls "$RONIVANS_BIN"/*.bin | head -1)" "RonivansLegacy_ChemicalProcessing.dll" -d "$TMPLIBS"

echo "==> Compiling C#..."
mkdir -p "$SCRIPT_DIR/dist"

mcs \
  -target:library \
  -out:"$SCRIPT_DIR/dist/MagpieExtension.dll" \
  -optimize+ \
  -langversion:7.2 \
  -r:"$ONI_MANAGED/Assembly-CSharp.dll" \
  -r:"$ONI_MANAGED/Assembly-CSharp-firstpass.dll" \
  -r:"$ONI_MANAGED/UnityEngine.CoreModule.dll" \
  -r:"$ONI_MANAGED/UnityEngine.dll" \
  -r:"$ONI_MANAGED/0Harmony.dll" \
  -r:"$ONI_MANAGED/netstandard.dll" \
  -r:"$TMPLIBS/PLib.dll" \
  -r:"$TMPLIBS/RonivansLegacy_ChemicalProcessing.dll" \
  "$SCRIPT_DIR/src/"*.cs

echo "==> Generating sprite assets..."
python3 "$SCRIPT_DIR/generate_sprites.py"

echo "==> Packaging mod..."
cp "$SCRIPT_DIR/mod.yaml" "$SCRIPT_DIR/dist/"
cp "$SCRIPT_DIR/mod_info.yaml" "$SCRIPT_DIR/dist/"
cp -r "$SCRIPT_DIR/anim/" "$SCRIPT_DIR/dist/" 2>/dev/null || true

echo ""
echo "=== Build complete! Files in: $SCRIPT_DIR/dist/ ==="
echo ""
echo "To install:"
echo "  mkdir -p ~/.config/unity3d/Klei/Oxygen\\ Not\\ Included/mods/Local/MagpieExtension"
echo "  cp -r $SCRIPT_DIR/dist/* ~/.config/unity3d/Klei/Oxygen\\ Not\\ Included/mods/Local/MagpieExtension/"
