using HarmonyLib;
using KMod;
using System.Reflection;

namespace MagpieExtension
{
    public class MagpieExtensionMod : UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            base.OnLoad(harmony);
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
    }

    // Register all 8 buildings on the plan screen, each after its 1-tile counterpart
    [HarmonyPatch(typeof(GeneratedBuildings), nameof(GeneratedBuildings.LoadGeneratedBuildings))]
    public static class GeneratedBuildings_Patch
    {
        public static void Prefix()
        {
            // HP gas bridges — insert after Ronivans' 1-tile HP gas bridge
            ModUtil.AddBuildingToPlanScreen("Utilities", HPGasConduitBridge2Config.ID, "HIGHPRESSUREGASCONDUITBRIDGE");
            ModUtil.AddBuildingToPlanScreen("Utilities", HPGasConduitBridge3Config.ID, HPGasConduitBridge2Config.ID);

            // HP liquid bridges
            ModUtil.AddBuildingToPlanScreen("Utilities", HPLiquidConduitBridge2Config.ID, "HIGHPRESSURELIQUIDCONDUITBRIDGE");
            ModUtil.AddBuildingToPlanScreen("Utilities", HPLiquidConduitBridge3Config.ID, HPLiquidConduitBridge2Config.ID);

            // Automation wire bridges — insert after vanilla 1-tile logic wire bridge
            ModUtil.AddBuildingToPlanScreen("Automation", LogicWireBridge2Config.ID, "LOGICWIREBRIDGE");
            ModUtil.AddBuildingToPlanScreen("Automation", LogicWireBridge3Config.ID, LogicWireBridge2Config.ID);

            // Automation ribbon bridges
            ModUtil.AddBuildingToPlanScreen("Automation", LogicRibbonBridge2Config.ID, "LOGICRIBBONBRIDGE");
            ModUtil.AddBuildingToPlanScreen("Automation", LogicRibbonBridge3Config.ID, LogicRibbonBridge2Config.ID);
        }
    }
}
