#!/usr/bin/env bash
# Build script for the consolidated "Magpie Bridges+" release.
#
# This mod ships THREE of your own DLLs together (ONI runs every UserMod2 it
# finds in a mod folder):
#   - MagpieExtension.dll          (automation/ribbon/conveyor gap bridges)
#   - MagpieExtensionRonivans.dll  (Ronivans Legacy bridges; soft-detected)
#   - MagpieBridgesPlusFixes.dll   (English names + correct categories for the
#                                   base Magpie Bridge's liquid/gas/wire bridges)
#
# It bundles NO third-party files. The base "Magpie Bridge (鹊桥)" mod is a
# Steam Workshop *Required Item* (it also provides PLib at runtime); Ronivans
# Legacy is optional.
set -e

ONI_MANAGED="$HOME/.steam/steam/steamapps/common/OxygenNotIncluded/OxygenNotIncluded_Data/Managed"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
DIST="$SCRIPT_DIR/dist"

echo "==> Building sibling source mods..."
( cd "$ROOT/MagpieExtension"         && ./build.sh >/dev/null )
( cd "$ROOT/MagpieExtensionRonivans" && ./build.sh >/dev/null )

echo "==> Compiling MagpieBridgesPlusFixes.dll..."
mkdir -p "$DIST"
mcs \
  -target:library \
  -out:"$DIST/MagpieBridgesPlusFixes.dll" \
  -optimize+ \
  -langversion:7.2 \
  -r:"$ONI_MANAGED/Assembly-CSharp.dll" \
  -r:"$ONI_MANAGED/Assembly-CSharp-firstpass.dll" \
  -r:"$ONI_MANAGED/UnityEngine.CoreModule.dll" \
  -r:"$ONI_MANAGED/UnityEngine.dll" \
  -r:"$ONI_MANAGED/0Harmony.dll" \
  -r:"$ONI_MANAGED/netstandard.dll" \
  "$SCRIPT_DIR/src/"*.cs

echo "==> Assembling dist/ ..."
cp "$ROOT/MagpieExtension/dist/MagpieExtension.dll"                 "$DIST/"
cp "$ROOT/MagpieExtensionRonivans/dist/MagpieExtensionRonivans.dll" "$DIST/"
# Merge anim assets from both extensions (fix DLL needs none).
mkdir -p "$DIST/anim"
cp -r "$ROOT/MagpieExtension/dist/anim/."         "$DIST/anim/" 2>/dev/null || true
cp -r "$ROOT/MagpieExtensionRonivans/dist/anim/." "$DIST/anim/" 2>/dev/null || true
cp "$SCRIPT_DIR/mod.yaml"      "$DIST/"
cp "$SCRIPT_DIR/mod_info.yaml" "$DIST/"
# preview.png (if present) is left in dist/ for the Workshop uploader.

echo ""
echo "=== Build complete. Contents of $DIST: ==="
ls -1 "$DIST"
echo ""
echo "To test locally:"
echo "  rm -rf ~/.config/unity3d/Klei/Oxygen\\ Not\\ Included/mods/Local/MagpieBridgesPlus"
echo "  cp -r \"$DIST\" ~/.config/unity3d/Klei/Oxygen\\ Not\\ Included/mods/Local/MagpieBridgesPlus"
echo "  (and make sure the base Magpie Bridge mod is subscribed/enabled)"
echo ""
echo "To publish to Steam Workshop:  ./publish.sh"
