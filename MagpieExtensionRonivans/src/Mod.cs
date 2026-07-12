using HarmonyLib;
using KMod;
using System.Reflection;
using TUNING;
using UnityEngine;
using BUILDINGS = TUNING.BUILDINGS;

namespace MagpieExtensionRonivans
{
    public class MagpieExtensionRonivansMod : UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            base.OnLoad(harmony);
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
    }

    [HarmonyPatch(typeof(GeneratedBuildings), nameof(GeneratedBuildings.LoadGeneratedBuildings))]
    public static class GeneratedBuildings_Patch
    {
        private const string SUBCATEGORY_CONVEYANCE = "conveyancestructures";

        private static bool _added;
        // Postfix (not Prefix) so we run AFTER Ronivans Legacy has registered its
        // own buildings -- otherwise our anchor IDs ("LogisticBridge",
        // "HPA_SolidRailBridge", etc.) don't exist yet and ONI falls back to
        // appending at the end of the plan list (wrong position).
        public static void Postfix()
        {
            // Guard: LoadGeneratedBuildings can fire multiple times during mod load.
            if (_added) return;
            _added = true;

            // Ronivans' DLL can be absent for one boot while Steam reinstalls an
            // update ("Latent reinstall" in Player.log). The configs registered
            // placeholder deprecated defs in that case; keep them out of the
            // plan screen too.
            if (!RonivansHelpers.RonivansLoaded)
            {
                Debug.LogWarning("[MagpieExtensionRonivans] Ronivans Legacy not loaded -- "
                                 + "skipping plan screen entries this boot.");
                return;
            }

            RegisterStrings();

            // Place six bridges in the Shipping > conveyancestructures section,
            // anchored after their respective Ronivans base bridge. The
            // Heavy-Duty Joint Plate variants use CreateFoundationTileDef
            // (see HPARailBridgeTile2Config) to set up the tile-foundation
            // fields the base Ronivans config needs.
            BUILDINGS.PLANSUBCATEGORYSORTING[LogisticBridge2Config.ID]      = SUBCATEGORY_CONVEYANCE;
            BUILDINGS.PLANSUBCATEGORYSORTING[LogisticBridge3Config.ID]      = SUBCATEGORY_CONVEYANCE;
            BUILDINGS.PLANSUBCATEGORYSORTING[HPARailBridge2Config.ID]       = SUBCATEGORY_CONVEYANCE;
            BUILDINGS.PLANSUBCATEGORYSORTING[HPARailBridge3Config.ID]       = SUBCATEGORY_CONVEYANCE;
            BUILDINGS.PLANSUBCATEGORYSORTING[HPARailBridgeTile2Config.ID]   = SUBCATEGORY_CONVEYANCE;
            BUILDINGS.PLANSUBCATEGORYSORTING[HPARailBridgeTile3Config.ID]   = SUBCATEGORY_CONVEYANCE;

            ModUtil.AddBuildingToPlanScreen("Conveyance", LogisticBridge2Config.ID, SUBCATEGORY_CONVEYANCE, "LogisticBridge");
            ModUtil.AddBuildingToPlanScreen("Conveyance", LogisticBridge3Config.ID, SUBCATEGORY_CONVEYANCE, LogisticBridge2Config.ID);
            ModUtil.AddBuildingToPlanScreen("Conveyance", HPARailBridge2Config.ID,  SUBCATEGORY_CONVEYANCE, "HPA_SolidRailBridge");
            ModUtil.AddBuildingToPlanScreen("Conveyance", HPARailBridge3Config.ID,  SUBCATEGORY_CONVEYANCE, HPARailBridge2Config.ID);
            ModUtil.AddBuildingToPlanScreen("Conveyance", HPARailBridgeTile2Config.ID, SUBCATEGORY_CONVEYANCE, "HPA_SolidRailBridgeTile");
            ModUtil.AddBuildingToPlanScreen("Conveyance", HPARailBridgeTile3Config.ID, SUBCATEGORY_CONVEYANCE, HPARailBridgeTile2Config.ID);

            // High Pressure gas/liquid bridges live in the HVAC / Plumbing menus,
            // anchored after Ronivans' own HP bridge.
            BUILDINGS.PLANSUBCATEGORYSORTING[HPGasBridge2Config.ID]    = "pipes";
            BUILDINGS.PLANSUBCATEGORYSORTING[HPGasBridge3Config.ID]    = "pipes";
            BUILDINGS.PLANSUBCATEGORYSORTING[HPLiquidBridge2Config.ID] = "pipes";
            BUILDINGS.PLANSUBCATEGORYSORTING[HPLiquidBridge3Config.ID] = "pipes";
            ModUtil.AddBuildingToPlanScreen("HVAC", HPGasBridge2Config.ID, "pipes", "HighPressureGasConduitBridge");
            ModUtil.AddBuildingToPlanScreen("HVAC", HPGasBridge3Config.ID, "pipes", HPGasBridge2Config.ID);
            ModUtil.AddBuildingToPlanScreen("Plumbing", HPLiquidBridge2Config.ID, "pipes", "HighPressureLiquidConduitBridge");
            ModUtil.AddBuildingToPlanScreen("Plumbing", HPLiquidBridge3Config.ID, "pipes", HPLiquidBridge2Config.ID);
        }

        private static void RegisterStrings()
        {
            Add("LOGISTICBRIDGE2",
                "Logistic Solid Bridge (2-Tile Gap)",
                "A 4-tile-long HP solid conveyor bridge that lets one rail cross over another with a 2-tile gap.",
                "Carries Solid Materials at HP throughput across other rails without connecting to them.");
            Add("LOGISTICBRIDGE3",
                "Logistic Solid Bridge (3-Tile Gap)",
                "A 5-tile-long HP solid conveyor bridge that lets one rail cross over another with a 3-tile gap.",
                "Carries Solid Materials at HP throughput across other rails without connecting to them.");
            Add("HPARAILBRIDGE2",
                "Heavy-Duty Bridge (2-Tile Gap)",
                "A 4-tile-long Heavy-Duty rail bridge that lets one HD rail cross over another with a 2-tile gap.",
                "Carries Solid Materials across other HD rails without connecting to them.");
            Add("HPARAILBRIDGE3",
                "Heavy-Duty Bridge (3-Tile Gap)",
                "A 5-tile-long Heavy-Duty rail bridge that lets one HD rail cross over another with a 3-tile gap.",
                "Carries Solid Materials across other HD rails without connecting to them.");
            Add("HPARAILBRIDGETILE2",
                "Heavy-Duty Joint Plate (2-Tile Gap)",
                "A 4-tile-long Heavy-Duty joint plate that lets one HD rail cross through walls with a 2-tile gap.",
                "Carries Solid Materials through wall and floor tile without connecting to other rails.");
            Add("HPGASBRIDGE2",
                "High Pressure Gas Bridge (2-Tile Gap)",
                "A 4-tile-long high pressure gas bridge that crosses a 2-tile gap.",
                "Carries Gas at high pressure across other pipes without connecting to them.");
            Add("HPGASBRIDGE3",
                "High Pressure Gas Bridge (3-Tile Gap)",
                "A 5-tile-long high pressure gas bridge that crosses a 3-tile gap.",
                "Carries Gas at high pressure across other pipes without connecting to them.");
            Add("HPLIQUIDBRIDGE2",
                "High Pressure Liquid Bridge (2-Tile Gap)",
                "A 4-tile-long high pressure liquid bridge that crosses a 2-tile gap.",
                "Carries Liquid at high pressure across other pipes without connecting to them.");
            Add("HPLIQUIDBRIDGE3",
                "High Pressure Liquid Bridge (3-Tile Gap)",
                "A 5-tile-long high pressure liquid bridge that crosses a 3-tile gap.",
                "Carries Liquid at high pressure across other pipes without connecting to them.");
            Add("HPARAILBRIDGETILE3",
                "Heavy-Duty Joint Plate (3-Tile Gap)",
                "A 5-tile-long Heavy-Duty joint plate that lets one HD rail cross through walls with a 3-tile gap.",
                "Carries Solid Materials through wall and floor tile without connecting to other rails.");
        }

        private static void Add(string idUpper, string name, string desc, string effect)
        {
            string prefix = "STRINGS.BUILDINGS.PREFABS." + idUpper + ".";
            Strings.Add(prefix + "NAME", name);
            Strings.Add(prefix + "DESC", desc);
            Strings.Add(prefix + "EFFECT", effect);
        }
    }

    // Same late re-glue pass as the base MagpieExtension DLL: other mods can
    // insert next to our anchors after we do, splitting the gap variants away
    // from their Ronivans counterparts. PlanScreen is created at world load,
    // after every mod's build-menu edits, so repositioning here always sticks.
    [HarmonyPatch(typeof(PlanScreen), "OnPrefabInit")]
    public static class PlanScreen_OnPrefabInit_Patch
    {
        public static void Prefix()
        {
            if (!RonivansHelpers.RonivansLoaded) return;
            Reposition("Conveyance", "LogisticBridge",              LogisticBridge2Config.ID);
            Reposition("Conveyance", LogisticBridge2Config.ID,      LogisticBridge3Config.ID);
            Reposition("Conveyance", "HPA_SolidRailBridge",         HPARailBridge2Config.ID);
            Reposition("Conveyance", HPARailBridge2Config.ID,       HPARailBridge3Config.ID);
            Reposition("Conveyance", "HPA_SolidRailBridgeTile",     HPARailBridgeTile2Config.ID);
            Reposition("Conveyance", HPARailBridgeTile2Config.ID,   HPARailBridgeTile3Config.ID);
            Reposition("HVAC",       "HighPressureGasConduitBridge", HPGasBridge2Config.ID);
            Reposition("HVAC",       HPGasBridge2Config.ID,          HPGasBridge3Config.ID);
            Reposition("Plumbing",   "HighPressureLiquidConduitBridge", HPLiquidBridge2Config.ID);
            Reposition("Plumbing",   HPLiquidBridge2Config.ID,          HPLiquidBridge3Config.ID);
        }

        // Moves ourId's plan-menu entry to directly after anchorId, in the same
        // per-category list ModUtil.AddBuildingToPlanScreen inserts into. No-op
        // when either entry is missing; idempotent when already in place.
        private static void Reposition(HashedString category, string anchorId, string ourId)
        {
            int ci = BUILDINGS.PLANORDER.FindIndex(p => p.category == category);
            if (ci < 0) return;
            var data = BUILDINGS.PLANORDER[ci].buildingAndSubcategoryData;
            int oi = data.FindIndex(kv => kv.Key == ourId);
            if (oi < 0 || data.FindIndex(kv => kv.Key == anchorId) < 0) return;
            var entry = data[oi];
            data.RemoveAt(oi);
            data.Insert(data.FindIndex(kv => kv.Key == anchorId) + 1, entry);
        }
    }
}
