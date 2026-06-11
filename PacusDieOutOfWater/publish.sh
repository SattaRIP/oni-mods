#!/usr/bin/env bash
# Publish PacusDieOutOfWater to Steam Workshop via steamcmd.
#
# Initial publish:
#   ./publish.sh
#   -> steamcmd prompts for Steam username, password, and 2FA code.
#   -> Returns a PublishedFileId (the Workshop item ID).
#   -> Add that ID to workshop_item.vdf manually (see comment below) so future
#      runs UPDATE the existing item instead of creating a new one.
#
# Subsequent updates (after publishedfileid is in the VDF):
#   ./publish.sh
#   -> Updates the existing Workshop item with current dist/ contents.
#
# Tips:
#  - Steam Guard 2FA: steamcmd asks once interactively; subsequent runs in the
#    same hour-ish should not re-prompt.
#  - To swap visibility, edit "visibility" in the VDF:
#      0 = public, 1 = friends-only, 2 = hidden/private  (current: 2)
#  - Re-run build.sh first if you changed source code.

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
VDF="$SCRIPT_DIR/workshop_item.vdf"
CONTENT="$SCRIPT_DIR/dist"
PREVIEW="$CONTENT/preview.png"

# Sanity checks
if [ ! -f "$VDF" ]; then
  echo "ERROR: workshop_item.vdf not found at $VDF" >&2
  exit 1
fi

if [ ! -d "$CONTENT" ] || [ -z "$(ls -A "$CONTENT" 2>/dev/null)" ]; then
  echo "ERROR: $CONTENT is missing or empty. Run build.sh first." >&2
  exit 1
fi

if [ ! -f "$PREVIEW" ]; then
  echo "WARNING: preview.png not found at $PREVIEW"
  echo "         Steam will use a default placeholder image."
  echo "         Press Ctrl+C to cancel, or any key to continue without preview."
  read -n 1 -s
  # If no preview, strip the previewfile line from VDF on the fly
  TMP_VDF="$(mktemp)"
  trap 'rm -f "$TMP_VDF"' EXIT
  grep -v '"previewfile"' "$VDF" > "$TMP_VDF"
  VDF="$TMP_VDF"
fi

echo "==> Publishing PacusDieOutOfWater to Steam Workshop..."
echo "    Content folder: $CONTENT"
echo "    VDF:            $VDF"
echo ""
echo "    Steam will prompt for username, password, and a Steam Guard code."
echo "    For ONI Workshop uploads you must use the Steam account that owns ONI."
echo ""

read -p "Steam username: " STEAM_USER

steamcmd \
  +login "$STEAM_USER" \
  +workshop_build_item "$VDF" \
  +quit

echo ""
echo "==> Done."
echo "    If this was an INITIAL publish, look above for a line like:"
echo "        'PublishFileID  XXXXXXXXXX'"
echo "    and add it to workshop_item.vdf inside the workshopitem block:"
echo "        \"publishedfileid\"  \"XXXXXXXXXX\""
echo "    so the next run updates this item instead of creating a duplicate."
