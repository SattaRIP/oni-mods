using HarmonyLib;

namespace MagpieExtensionRonivans
{
    // Our wider bridges use centered placement offsets (e.g. [-1..2] for
    // 4-wide). Near the grid's left/right edge, raw cell arithmetic can produce
    // out-of-range cells (negative on row 0), and Constructable.PlaceDiggables /
    // Diggable.OnCleanUp then throw IndexOutOfRangeException on every dig event
    // -- see MORNING_NOTES 2026-06-03.
    //
    // This is Option C from those notes: a minimal prefix on the small static
    // Diggable.IsDiggable(int) that answers "not diggable" for off-grid cells
    // instead of letting Grid array accesses throw. We deliberately do NOT
    // patch BuildingDef.IsValidPlaceLocation -- that method accumulates patches
    // from several mods and rebuilding it triggered the Mono dynamic-method
    // "Method has zero rva" crash (BadImageFormatException) on this install.
    [HarmonyPatch(typeof(Diggable), nameof(Diggable.IsDiggable), new[] { typeof(int) })]
    public static class PlacementGuard
    {
        public static bool Prefix(int cell, ref bool __result)
        {
            if (!Grid.IsValidCell(cell))
            {
                __result = false;
                return false; // skip original; off-grid is never diggable
            }
            return true;
        }
    }
}
