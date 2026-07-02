using HarmonyLib;

namespace MagpieExtension
{
    // Harmony wiring for WideLogicBridgeManager. No new component type is ever added to
    // a prefab (that path crashed spawn with Mono "Method has zero rva"), so the entire
    // automation-overlay tinting feature is driven from these four postfixes.
    //
    // Method names are passed as strings because Building.OnSpawn/OnCleanUp and the
    // overlay Update/Disable are non-public overrides; Harmony resolves them by name.

    [HarmonyPatch(typeof(Building), "OnSpawn")]
    internal static class Building_OnSpawn_TintRegister
    {
        public static void Postfix(Building __instance)
        {
            WideLogicBridgeManager.TryRegister(__instance.gameObject);
        }
    }

    [HarmonyPatch(typeof(Building), "OnCleanUp")]
    internal static class Building_OnCleanUp_TintUnregister
    {
        public static void Postfix(Building __instance)
        {
            WideLogicBridgeManager.Unregister(__instance.gameObject);
        }
    }

    [HarmonyPatch(typeof(OverlayModes.Logic), "Update")]
    internal static class LogicOverlay_Update_Tint
    {
        public static void Postfix()
        {
            WideLogicBridgeManager.ApplyTints();
        }
    }

    [HarmonyPatch(typeof(OverlayModes.Logic), "Disable")]
    internal static class LogicOverlay_Disable_Reset
    {
        public static void Postfix()
        {
            WideLogicBridgeManager.ResetTints();
        }
    }
}
