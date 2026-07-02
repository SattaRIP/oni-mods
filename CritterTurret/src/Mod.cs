using HarmonyLib;
using KMod;
using System.Reflection;
using BUILDINGS = TUNING.BUILDINGS;

namespace CritterTurret
{
    public class CritterTurretMod : UserMod2
    {
        public override void OnLoad(Harmony harmony)
        {
            base.OnLoad(harmony);
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
    }

    // IBuildingConfig subclasses are auto-discovered/registered by the game.
    // Here we only add the building's strings and its build-menu placement.
    [HarmonyPatch(typeof(GeneratedBuildings), nameof(GeneratedBuildings.LoadGeneratedBuildings))]
    public static class GeneratedBuildings_Patch
    {
        private static bool _added;

        public static void Prefix()
        {
            if (_added) return;
            _added = true;

            string p = "STRINGS.BUILDINGS.PREFABS." + CritterTurretConfig.ID.ToUpperInvariant() + ".";
            Strings.Add(p + "NAME",
                "Critter Turret");
            Strings.Add(p + "DESC",
                "A red robo-miner reconfigured to fire on critters instead of mining. It mounts on a floor, wall, or ceiling and shoots within a directional arc (line of sight required).");
            Strings.Add(p + "EFFECT",
                "Automatically attacks critters within its room, culling surplus population down to a threshold you set. Can target adults, babies, or both.");

            // The threshold side screen's Title/ValueName are LocStrings, whose 1-arg
            // constructor stores the string as a lookup KEY (not text) -- so unless the
            // key is registered it renders as "MISSING.". Register key == text.
            Strings.Add("STRINGS.UI.UISIDESCREENS.CRITTERTURRET.OPEN_FIRE", "Open Fire At Population");
            Strings.Add("STRINGS.UI.UISIDESCREENS.CRITTERTURRET.VALUE_NAME", "Critters in room");

            // Same category + subcategory + menu position as the Robo-Miner (Auto-Miner):
            // category "Conveyance" (the Shipping tab), subcategory "automated", placed
            // right next to "AutoMiner".
            BUILDINGS.PLANSUBCATEGORYSORTING[CritterTurretConfig.ID] = "automated";
            ModUtil.AddBuildingToPlanScreen("Conveyance", CritterTurretConfig.ID, "automated", "AutoMiner");
        }
    }
}
