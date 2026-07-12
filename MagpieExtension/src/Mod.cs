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
        private const string SUBCATEGORY_PIPES      = "pipes";

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
            // Standalone plumbing/power bridges: pipes under Plumbing/HVAC, wires under Power.
            BUILDINGS.PLANSUBCATEGORYSORTING[LiquidConduitBridge2Config.ID]  = SUBCATEGORY_PIPES;
            BUILDINGS.PLANSUBCATEGORYSORTING[LiquidConduitBridge3Config.ID]  = SUBCATEGORY_PIPES;
            BUILDINGS.PLANSUBCATEGORYSORTING[GasConduitBridge2Config.ID]     = SUBCATEGORY_PIPES;
            BUILDINGS.PLANSUBCATEGORYSORTING[GasConduitBridge3Config.ID]     = SUBCATEGORY_PIPES;
            BUILDINGS.PLANSUBCATEGORYSORTING[WireBridge2Config.ID]           = SUBCATEGORY_WIRES;
            BUILDINGS.PLANSUBCATEGORYSORTING[WireBridge3Config.ID]           = SUBCATEGORY_WIRES;
            BUILDINGS.PLANSUBCATEGORYSORTING[WireRefinedBridge2Config.ID]    = SUBCATEGORY_WIRES;
            BUILDINGS.PLANSUBCATEGORYSORTING[WireRefinedBridge3Config.ID]    = SUBCATEGORY_WIRES;
            BUILDINGS.PLANSUBCATEGORYSORTING[WireRubberBridge2Config.ID]     = SUBCATEGORY_WIRES;
            BUILDINGS.PLANSUBCATEGORYSORTING[WireRubberBridge3Config.ID]     = SUBCATEGORY_WIRES;

            // 4-arg overload: (category, building_id, subcategoryID, relativeBuildingId).
            ModUtil.AddBuildingToPlanScreen("Automation", LogicWireBridge2Config.ID,    SUBCATEGORY_WIRES,      "LogicWireBridge");
            ModUtil.AddBuildingToPlanScreen("Automation", LogicWireBridge3Config.ID,    SUBCATEGORY_WIRES,      LogicWireBridge2Config.ID);
            ModUtil.AddBuildingToPlanScreen("Automation", LogicRibbonBridge2Config.ID,  SUBCATEGORY_WIRES,      "LogicRibbonBridge");
            ModUtil.AddBuildingToPlanScreen("Automation", LogicRibbonBridge3Config.ID,  SUBCATEGORY_WIRES,      LogicRibbonBridge2Config.ID);
            // Plan-menu category for the Shipping tab is internally "Conveyance" --
            // "Shipping" is only the display label in the bottom bar UI.
            ModUtil.AddBuildingToPlanScreen("Conveyance", SolidConduitBridge2Config.ID, SUBCATEGORY_CONVEYANCE, "SolidConduitBridge");
            ModUtil.AddBuildingToPlanScreen("Conveyance", SolidConduitBridge3Config.ID, SUBCATEGORY_CONVEYANCE, SolidConduitBridge2Config.ID);

            // Standalone vanilla-style bridges, each next to its 1-tile counterpart.
            ModUtil.AddBuildingToPlanScreen("Plumbing", LiquidConduitBridge2Config.ID, SUBCATEGORY_PIPES, "LiquidConduitBridge");
            ModUtil.AddBuildingToPlanScreen("Plumbing", LiquidConduitBridge3Config.ID, SUBCATEGORY_PIPES, LiquidConduitBridge2Config.ID);
            ModUtil.AddBuildingToPlanScreen("HVAC",     GasConduitBridge2Config.ID,    SUBCATEGORY_PIPES, "GasConduitBridge");
            ModUtil.AddBuildingToPlanScreen("HVAC",     GasConduitBridge3Config.ID,    SUBCATEGORY_PIPES, GasConduitBridge2Config.ID);
            ModUtil.AddBuildingToPlanScreen("Power",    WireBridge2Config.ID,          SUBCATEGORY_WIRES, "WireBridge");
            ModUtil.AddBuildingToPlanScreen("Power",    WireBridge3Config.ID,          SUBCATEGORY_WIRES, WireBridge2Config.ID);
            ModUtil.AddBuildingToPlanScreen("Power",    WireRefinedBridge2Config.ID,   SUBCATEGORY_WIRES, "WireRefinedBridge");
            ModUtil.AddBuildingToPlanScreen("Power",    WireRefinedBridge3Config.ID,   SUBCATEGORY_WIRES, WireRefinedBridge2Config.ID);
            ModUtil.AddBuildingToPlanScreen("Power",    WireRubberBridge2Config.ID,    SUBCATEGORY_WIRES, "WireRubberBridge");
            ModUtil.AddBuildingToPlanScreen("Power",    WireRubberBridge3Config.ID,    SUBCATEGORY_WIRES, WireRubberBridge2Config.ID);
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
            Add("SHUIGUANQIAO2",
                "Liquid Pipe Bridge (2-Tile Gap)",
                "A 4-tile-long liquid pipe bridge that lets one pipe cross over another with a 2-tile gap.",
                "Carries liquid across other pipes without connecting to them.");
            Add("SHUIGUANQIAO3",
                "Liquid Pipe Bridge (3-Tile Gap)",
                "A 5-tile-long liquid pipe bridge that lets one pipe cross over another with a 3-tile gap.",
                "Carries liquid across other pipes without connecting to them.");
            Add("QIGUANQIAO2",
                "Gas Pipe Bridge (2-Tile Gap)",
                "A 4-tile-long gas pipe bridge that lets one pipe cross over another with a 2-tile gap.",
                "Carries gas across other pipes without connecting to them.");
            Add("QIGUANQIAO3",
                "Gas Pipe Bridge (3-Tile Gap)",
                "A 5-tile-long gas pipe bridge that lets one pipe cross over another with a 3-tile gap.",
                "Carries gas across other pipes without connecting to them.");
            Add("DIANXIANQIAO2",
                "Wire Bridge (2-Tile Gap)",
                "A 4-tile-long wire bridge that lets one power wire cross over another with a 2-tile gap.",
                "Carries power across other wires without connecting their circuits.");
            Add("DIANXIANQIAO3",
                "Wire Bridge (3-Tile Gap)",
                "A 5-tile-long wire bridge that lets one power wire cross over another with a 3-tile gap.",
                "Carries power across other wires without connecting their circuits.");
            Add("DAOXIANQIAO2",
                "Conductive Wire Bridge (2-Tile Gap)",
                "A 4-tile-long conductive wire bridge that lets one conductive wire cross over another with a 2-tile gap.",
                "Carries high-wattage power across other wires without connecting their circuits.");
            Add("DAOXIANQIAO3",
                "Conductive Wire Bridge (3-Tile Gap)",
                "A 5-tile-long conductive wire bridge that lets one conductive wire cross over another with a 3-tile gap.",
                "Carries high-wattage power across other wires without connecting their circuits.");
            Add("WIRERUBBERBRIDGE2",
                "Insulated Conductive Wire Bridge (2-Tile Gap)",
                "A 4-tile-long insulated conductive wire bridge that lets one wire cross over another with a 2-tile gap.",
                "Carries high-wattage power across other wires without joining them. Can be run through tile.");
            Add("WIRERUBBERBRIDGE3",
                "Insulated Conductive Wire Bridge (3-Tile Gap)",
                "A 5-tile-long insulated conductive wire bridge that lets one wire cross over another with a 3-tile gap.",
                "Carries high-wattage power across other wires without joining them. Can be run through tile.");
        }

        private static void Add(string idUpper, string name, string desc, string effect)
        {
            string prefix = "STRINGS.BUILDINGS.PREFABS." + idUpper + ".";
            Strings.Add(prefix + "NAME", name);
            Strings.Add(prefix + "DESC", desc);
            Strings.Add(prefix + "EFFECT", effect);
        }
    }

    // Other mods (e.g. Ronivans' High Pressure conduits) also insert themselves
    // right after the vanilla bridges during mod load; whichever mod runs later
    // wins the slot, splitting our gap variants away from their vanilla
    // counterparts. PlanScreen is created at world load, after every mod's
    // build-menu edits, so re-gluing the variants to their anchors here always
    // sticks (interlopers get pushed after our variants instead).
    [HarmonyPatch(typeof(PlanScreen), "OnPrefabInit")]
    public static class PlanScreen_OnPrefabInit_Patch
    {
        public static void Prefix()
        {
            Reposition("Automation", "LogicWireBridge",           LogicWireBridge2Config.ID);
            Reposition("Automation", LogicWireBridge2Config.ID,   LogicWireBridge3Config.ID);
            Reposition("Automation", "LogicRibbonBridge",         LogicRibbonBridge2Config.ID);
            Reposition("Automation", LogicRibbonBridge2Config.ID, LogicRibbonBridge3Config.ID);
            Reposition("Conveyance", "SolidConduitBridge",        SolidConduitBridge2Config.ID);
            Reposition("Conveyance", SolidConduitBridge2Config.ID, SolidConduitBridge3Config.ID);
            Reposition("Plumbing",   "LiquidConduitBridge",       LiquidConduitBridge2Config.ID);
            Reposition("Plumbing",   LiquidConduitBridge2Config.ID, LiquidConduitBridge3Config.ID);
            Reposition("HVAC",       "GasConduitBridge",          GasConduitBridge2Config.ID);
            Reposition("HVAC",       GasConduitBridge2Config.ID,  GasConduitBridge3Config.ID);
            Reposition("Power",      "WireBridge",                WireBridge2Config.ID);
            Reposition("Power",      WireBridge2Config.ID,        WireBridge3Config.ID);
            Reposition("Power",      "WireRefinedBridge",         WireRefinedBridge2Config.ID);
            Reposition("Power",      WireRefinedBridge2Config.ID, WireRefinedBridge3Config.ID);
            Reposition("Power",      "WireRubberBridge",          WireRubberBridge2Config.ID);
            Reposition("Power",      WireRubberBridge2Config.ID,  WireRubberBridge3Config.ID);
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
