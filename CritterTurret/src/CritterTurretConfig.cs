using System;
using System.Collections.Generic;
using TUNING;
using UnityEngine;
using BUILDINGS = TUNING.BUILDINGS;

namespace CritterTurret
{
    // Milestone 0: a "red Auto-Miner" shell. Clones the vanilla Auto-Miner building
    // definition exactly (2x2, auto_miner_kanim, 120 W, OnFoundationRotatable, R360,
    // logic on/off port), recolours it red, and attaches a minimal custom component
    // to test whether custom KMonoBehaviours place without the "zero rva" JIT crash
    // seen on this install. No targeting/firing yet.
    public class CritterTurretConfig : IBuildingConfig
    {
        public const string ID = "CritterTurret";

        public override BuildingDef CreateBuildingDef()
        {
            BuildingDef def = BuildingTemplates.CreateBuildingDef(
                ID, 2, 2, "critterturret_kanim", 10, 10f,
                BUILDINGS.CONSTRUCTION_MASS_KG.TIER3, MATERIALS.REFINED_METALS, 1600f,
                BuildLocationRule.OnFoundationRotatable, BUILDINGS.DECOR.PENALTY.TIER2,
                NOISE_POLLUTION.NOISY.TIER0, 0.2f);
            def.Floodable = false;
            def.AudioCategory = "Metal";
            def.RequiresPowerInput = true;
            def.EnergyConsumptionWhenActive = 120f;
            def.ExhaustKilowattsWhenActive = 0f;
            def.SelfHeatKilowattsWhenActive = 2f;
            def.PermittedRotations = PermittedRotations.R360;
            def.LogicInputPorts = LogicOperationalController.CreateSingleInputPortList(new CellOffset(0, 0));
            return def;
        }

        public override void ConfigureBuildingTemplate(GameObject go, Tag prefab_tag)
        {
            go.AddOrGet<Operational>();

            // Species checklist -- the same TreeFilterableSideScreen the Critter
            // Drop-Off shows. The screen requires a Storage on the target and reads
            // its storageFilters for the category rows; this Storage never actually
            // stores anything (nothing ever fetches to it). Rows list every
            // DISCOVERED species under each category, so wranglable land critters
            // and catchable fish all show up; the brain checks targets against
            // TreeFilterable's accepted tags.
            Storage storage = go.AddOrGet<Storage>();
            storage.allowItemRemoval = false;
            storage.showDescriptor = false;
            storage.allowSettingOnlyFetchMarkedItems = false;
            storage.storageFilters = new List<Tag>
            {
                GameTags.BagableCreature,
                GameTags.SwimmingCreature,
                CritterTurretTags.OtherCritters
            };

            TreeFilterable filterable = go.AddOrGet<TreeFilterable>();
            // Storage bins tint themselves + raise "no storage filter set" when
            // nothing is checked; on a turret that state just means "hold fire at
            // listed species", so keep the building's normal look.
            filterable.tintOnNoFiltersSet = false;
            // Would prune accepted tags against the category table on spawn; our
            // OtherCritters category registers per-game after load, so pruning
            // could race it and silently drop saved selections.
            filterable.filterByStorageCategoriesOnSpawn = false;
        }

        // The turret's kanim has red baked into the atlas, which fights ONI's
        // placement feedback: the drag preview is tinted by multiplication (white =
        // valid, red = invalid), and red art times white tint still reads as red.
        // Give just the preview the vanilla grey Auto-Miner art so valid/invalid
        // placement shows normally; the menu icon and built turret stay red.
        // NOTE: the preview/under-construction prefabs' anim controllers are already
        // initialized (Util.PreInit inside BuildingLoader.CreateBuilding*) by the time
        // these hooks run, so just assigning AnimFiles does nothing -- SwapAnims is the
        // rebuild-and-rebind path.
        private static void SwapToVanillaAnim(GameObject go, string context)
        {
            try
            {
                var kbac = go.GetComponent<KBatchedAnimController>();
                KAnimFile vanilla = Assets.GetAnim("auto_miner_kanim");
                if (kbac == null || vanilla == null)
                {
                    Debug.LogWarning("[CritterTurret] " + context + " anim swap skipped (controller=" + (kbac != null) + ", anim=" + (vanilla != null) + ")");
                    return;
                }
                kbac.SwapAnims(new KAnimFile[] { vanilla });
                Debug.Log("[CritterTurret] " + context + " anim swapped to vanilla auto_miner for placement tinting");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[CritterTurret] " + context + " anim swap failed (stays red): " + e);
            }
        }

        public override void DoPostConfigurePreview(BuildingDef def, GameObject go)
        {
            SwapToVanillaAnim(go, "preview");
        }

        public override void DoPostConfigureUnderConstruction(GameObject go)
        {
            SwapToVanillaAnim(go, "under-construction");
        }

        public override void DoPostConfigureComplete(GameObject go)
        {
            go.AddOrGet<LogicOperationalController>();
            // Plays the dupes' AttackLaser_gun loop while the turret is engaging.
            go.AddOrGet<LoopingSounds>();

            // Range visual: a forward fan (trimmed by line of sight), shown when the
            // turret is selected -- same RangeVisualizer the Auto-Miner uses. Must match
            // the brain's targeting cone (CritterTurretBrain.RANGE / arc).
            var rv = go.AddOrGet<RangeVisualizer>();
            rv.OriginOffset = new Vector2I(0, 0);
            rv.RangeMin = new Vector2I(CritterTurretBrain.RANGE_MIN_X, CritterTurretBrain.RANGE_MIN_Y);
            rv.RangeMax = new Vector2I(CritterTurretBrain.RANGE_MAX_X, CritterTurretBrain.RANGE_MAX_Y);
            rv.TestLineOfSight = true;
            rv.BlockingCb = (int cell) => !Grid.IsValidCell(cell) || Grid.Solid[cell];

            // The turret's targeting/firing brain. Custom components are safe on this
            // install (PacusDieOutOfWater ships one; the old "zero rva" crash came from
            // Harmony-rebuilding BuildingDef.IsValidPlaceLocation, not from components).
            go.AddOrGet<CritterTurretBrain>();
        }
    }
}
