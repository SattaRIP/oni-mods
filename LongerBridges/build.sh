#!/usr/bin/env bash
# Build script for the "Longer Bridges" release.
#
# This mod ships TWO of your own DLLs together (ONI runs every UserMod2 it
# finds in a mod folder):
#   - MagpieExtension.dll          (all the standalone gap bridges: liquid, gas,
#                                   power, conductive, insulated conductive,
#                                   automation wire/ribbon, conveyor)
#   - MagpieExtensionRonivans.dll  (Ronivans Legacy bridges; soft-detected)
#
# It is fully STANDALONE — bundles NO third-party files and requires no other
# mod. Ronivans Legacy is optional (its bridges appear automatically if present).
set -e

ONI_MANAGED="$HOME/.steam/steam/steamapps/common/OxygenNotIncluded/OxygenNotIncluded_Data/Managed"
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
DIST="$SCRIPT_DIR/dist"

echo "==> Building sibling source mods..."
( cd "$ROOT/MagpieExtension"         && ./build.sh >/dev/null )
( cd "$ROOT/MagpieExtensionRonivans" && ./build.sh >/dev/null )

echo "==> Assembling dist/ ..."
rm -rf "$DIST"
mkdir -p "$DIST/anim"
cp "$ROOT/MagpieExtension/dist/MagpieExtension.dll"                 "$DIST/"
cp "$ROOT/MagpieExtensionRonivans/dist/MagpieExtensionRonivans.dll" "$DIST/"
# Merge anim assets from BOTH extensions. Both are required: dropping the
# Ronivans anims makes ONI crash with "First anim file needs to be non-null".
cp -r "$ROOT/MagpieExtension/dist/anim/."         "$DIST/anim/" 2>/dev/null || true
cp -r "$ROOT/MagpieExtensionRonivans/dist/anim/." "$DIST/anim/" 2>/dev/null || true
cp "$SCRIPT_DIR/mod.yaml"      "$DIST/"
cp "$SCRIPT_DIR/mod_info.yaml" "$DIST/"
# Workshop preview image.
cp "$SCRIPT_DIR/preview.png"   "$DIST/" 2>/dev/null || true

echo ""
echo "=== Build complete. Contents of $DIST: ==="
ls -1 "$DIST"
echo ""
echo "To deploy as a dev mod for in-game upload:"
echo "  rm -rf ~/.config/unity3d/Klei/Oxygen\\ Not\\ Included/mods/Dev/LongerBridges"
echo "  cp -r \"$DIST\" ~/.config/unity3d/Klei/Oxygen\\ Not\\ Included/mods/Dev/LongerBridges"
echo ""
echo "Then launch ONI -> Mods -> Longer Bridges (Dev) -> Upload Mod."
echo "(steamcmd cannot publish ONI mods: 'no workshop depot found'.)"
