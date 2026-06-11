using HarmonyLib;
using KMod;
using System.Reflection;
using TUNING;

namespace MagpieExtension
{
    public class MagpieExtensionMod : UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            base.OnLoad(harmony);

            // History: OnLoad used to resolve Ronivans Legacy's HP registration API
            // by reflection and MagpieHPPatches.cs force-registered Magpie's normal
            // gas/liquid bridges (qiguanqiao2/3, shuiguanqiao2/3) as HP-capable, so
            // HP pipes could connect to them without damage. Removed 2026-06-11 at
            // the user's request: HP content entering normal Magpie bridges should
            // damage them, same as any other normal conduit (Ronivans' overpressure
            // rules now apply unmodified). See git history for the old code.

            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
    }

    [HarmonyPatch(typeof(GeneratedBuildings), nameof(GeneratedBuildings.LoadGeneratedBuildings))]
    public static class GeneratedBuildings_Patch
    {
        private const string SUBCATEGORY_WIRES      = "wires";
        private const string SUBCATEGORY_CONVEYANCE = "conveyancestructures";

        private static bool _added;
        public static void Prefix()
        {
            // Guard: LoadGeneratedBuildings can be invoked more than once during the
            // mod load sequence; double-adding causes the codex to complain about
            // duplicate entries ("Tried to add ... to the Codex screen multiple times").
            if (_added) return;
            _added = true;

            RegisterStrings();

            // Register subcategory placement so PlanScreen doesn't warn and the codex
            // sorter knows where they belong. Vanilla LogicWireBridge / LogicRibbonBridge
            // both live in "wires" under Automation; SolidConduitBridge lives in
            // "conveyancestructures" under Shipping.
            BUILDINGS.PLANSUBCATEGORYSORTING[LogicWireBridge2Config.ID]      = SUBCATEGORY_WIRES;
            BUILDINGS.PLANSUBCATEGORYSORTING[LogicWireBridge3Config.ID]      = SUBCATEGORY_WIRES;
            BUILDINGS.PLANSUBCATEGORYSORTING[LogicRibbonBridge2Config.ID]    = SUBCATEGORY_WIRES;
            BUILDINGS.PLANSUBCATEGORYSORTING[LogicRibbonBridge3Config.ID]    = SUBCATEGORY_WIRES;
            BUILDINGS.PLANSUBCATEGORYSORTING[SolidConduitBridge2Config.ID]   = SUBCATEGORY_CONVEYANCE;
            BUILDINGS.PLANSUBCATEGORYSORTING[SolidConduitBridge3Config.ID]   = SUBCATEGORY_CONVEYANCE;

            // 4-arg overload: (category, building_id, subcategoryID, relativeBuildingId).
            ModUtil.AddBuildingToPlanScreen("Automation", LogicWireBridge2Config.ID,    SUBCATEGORY_WIRES,      "LogicWireBridge");
            ModUtil.AddBuildingToPlanScreen("Automation", LogicWireBridge3Config.ID,    SUBCATEGORY_WIRES,      LogicWireBridge2Config.ID);
            ModUtil.AddBuildingToPlanScreen("Automation", LogicRibbonBridge2Config.ID,  SUBCATEGORY_WIRES,      "LogicRibbonBridge");
            ModUtil.AddBuildingToPlanScreen("Automation", LogicRibbonBridge3Config.ID,  SUBCATEGORY_WIRES,      LogicRibbonBridge2Config.ID);
            // Plan-menu category for the Shipping tab is internally "Conveyance" --
            // "Shipping" is only the display label in the bottom bar UI.
            ModUtil.AddBuildingToPlanScreen("Conveyance", SolidConduitBridge2Config.ID, SUBCATEGORY_CONVEYANCE, "SolidConduitBridge");
            ModUtil.AddBuildingToPlanScreen("Conveyance", SolidConduitBridge3Config.ID, SUBCATEGORY_CONVEYANCE, SolidConduitBridge2Config.ID);
        }

        private static void RegisterStrings()
        {
            Add("LOGICWIREBRIDGE2",
                "Automation Wire Bridge (2-Tile Gap)",
                "A 4-tile-long automation wire bridge that lets one signal cross over another with a 2-tile gap.",
                "Carries an automation signal across other wires without connecting to them.");
            Add("LOGICWIREBRIDGE3",
                "Automation Wire Bridge (3-Tile Gap)",
                "A 5-tile-long automation wire bridge that lets one signal cross over another with a 3-tile gap.",
                "Carries an automation signal across other wires without connecting to them.");
            Add("LOGICRIBBONBRIDGE2",
                "Automation Ribbon Bridge (2-Tile Gap)",
                "A 4-tile-long automation ribbon bridge that lets one ribbon cross over another with a 2-tile gap.",
                "Carries a four-channel automation ribbon across other wires without connecting to them.");
            Add("LOGICRIBBONBRIDGE3",
                "Automation Ribbon Bridge (3-Tile Gap)",
                "A 5-tile-long automation ribbon bridge that lets one ribbon cross over another with a 3-tile gap.",
                "Carries a four-channel automation ribbon across other wires without connecting to them.");
            Add("SOLIDCONDUITBRIDGE2",
                "Conveyor Bridge (2-Tile Gap)",
                "A 4-tile-long conveyor bridge that lets one rail cross over another with a 2-tile gap.",
                "Carries Solid Materials across other rails without connecting to them.");
            Add("SOLIDCONDUITBRIDGE3",
                "Conveyor Bridge (3-Tile Gap)",
                "A 5-tile-long conveyor bridge that lets one rail cross over another with a 3-tile gap.",
                "Carries Solid Materials across other rails without connecting to them.");
        }

        private static void Add(string idUpper, string name, string desc, string effect)
        {
            string prefix = "STRINGS.BUILDINGS.PREFABS." + idUpper + ".";
            Strings.Add(prefix + "NAME", name);
            Strings.Add(prefix + "DESC", desc);
            Strings.Add(prefix + "EFFECT", effect);
        }
    }
}
