using HarmonyLib;
using KMod;
using System.Reflection;
using UnityEngine;
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

    // The species checklist can only show species registered under a category in
    // DiscoveredResources. Wranglable (BagableCreature) and net-catchable
    // (SwimmingCreature) critters register themselves as they're encountered;
    // every other shootable critter (Beetas, hives, robots...) gets a row under
    // this category of ours so "checked = shot" holds for everything.
    public static class CritterTurretTags
    {
        // Second Create arg registers the display name the row header uses.
        public static readonly Tag OtherCritters = TagManager.Create("CritterTurretOther", "Other Critters");

        // DiscoverCategory is private; invoked by reflection rather than
        // duplicating its bookkeeping. Not a Harmony patch, so no rebuilt-IL risk.
        private static readonly MethodInfo DiscoverCategory =
            AccessTools.Method(typeof(DiscoveredResources), "DiscoverCategory");

        // Which per-game DiscoveredResources we've already registered into. Runs
        // again after every load because save deserialization rebuilds the
        // category table. Called from CritterTurretBrain.OnSpawn, which is always
        // after deserialization and before the side screen can target a turret.
        private static DiscoveredResources registeredFor;

        public static void EnsureOtherCrittersRegistered()
        {
            DiscoveredResources inst = DiscoveredResources.Instance;
            if (inst == null || inst == registeredFor || DiscoverCategory == null) return;
            registeredFor = inst;

            foreach (GameObject prefab in Assets.GetPrefabsWithComponent<CreatureBrain>())
            {
                if (prefab == null || prefab.GetComponent<Health>() == null) continue;
                KPrefabID kpid = prefab.GetComponent<KPrefabID>();
                if (kpid == null) continue;
                if (kpid.HasTag(GameTags.BagableCreature) || kpid.HasTag(GameTags.SwimmingCreature)) continue;
                // Robots (Biobots, Rovers, Flydos, Sweepys...) aren't critters;
                // the brain refuses to shoot them, so no checkbox either.
                if (kpid.HasTag(GameTags.Robot)) continue;
                DiscoverCategory.Invoke(inst, new object[] { OtherCritters, kpid.PrefabTag });
            }
        }
    }

    // Research: same node as the Critter Drop-Off and friends.
    [HarmonyPatch(typeof(Db), "Initialize")]
    public static class Db_Initialize_Patch
    {
        public static void Postfix()
        {
            Tech tech = Db.Get().Techs.TryGet("AnimalControl");
            if (tech != null)
                tech.unlockedItemIDs.Add(CritterTurretConfig.ID);
            else
                Debug.LogWarning("[CritterTurret] AnimalControl tech not found; building stays unlocked from the start");
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
                "Automatically attacks critters within its room, culling surplus population down to a threshold you set. Target species are picked from a checklist; babies are listed separately from adults.");

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
