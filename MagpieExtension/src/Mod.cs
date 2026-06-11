using HarmonyLib;
using KMod;
using System;
using System.Reflection;
using TUNING;
using UnityEngine;

namespace MagpieExtension
{
    public class MagpieExtensionMod : UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            base.OnLoad(harmony);

            // Resolve Ronivans Legacy's HP registration API by reflection so we don't
            // bind to it at compile time. If the mod isn't loaded, the patches simply
            // skip via their [HarmonyPrepare] guards and the wire/ribbon bridges still
            // work standalone.
            try
            {
                var asm = AppDomain.CurrentDomain.GetAssemblies();
                foreach (var a in asm)
                {
                    var t = a.GetType("RonivansLegacy_ChemicalProcessing.Content.Scripts.HighPressureConduitRegistration");
                    if (t != null)
                    {
                        MagpieBridges.RegistrationType = t;
                        MagpieBridges.RegisterMethod = t.GetMethod("RegisterHighPressureConduit", BindingFlags.Public | BindingFlags.Static);
                        // Cache the internal HashSet fields too so the cleanup path
                        // can manipulate them directly without going through Ronivans'
                        // UnregisterHighPressureConduit method (whose reflection invoke
                        // was tripping a Mono runtime UTF-8 decode failure).
                        const BindingFlags F = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
                        MagpieBridges.Field_AllHandles        = t.GetField("AllHighPressureConduitGOHandles", F);
                        MagpieBridges.Field_HPA_GasBridge     = t.GetField("HPA_GasBridge", F);
                        MagpieBridges.Field_HPA_LiquidBridge  = t.GetField("HPA_LiquidBridge", F);
                        MagpieBridges.Field_All_GasBridge     = t.GetField("All_GasBridge", F);
                        MagpieBridges.Field_All_LiquidBridge  = t.GetField("All_LiquidBridge", F);
                        break;
                    }
                }
                if (MagpieBridges.RegistrationType == null)
                    Debug.Log("[MagpieExtension] Ronivans Legacy not detected -- HP bridge patches inactive (wire/ribbon bridges still load).");
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[MagpieExtension] Failed to resolve Ronivans HP API: " + ex.Message);
            }

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
