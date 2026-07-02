using System;
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
                ID, 2, 2, "auto_miner_kanim", 10, 10f,
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
        }

        public override void DoPostConfigureComplete(GameObject go)
        {
            go.AddOrGet<LogicOperationalController>();
            // Plays the dupes' AttackLaser_gun loop while the turret is engaging.
            go.AddOrGet<LoopingSounds>();

            // Prototype recolour: tint the orange Auto-Miner art toward red.
            // (A proper recoloured kanim comes later.)
            var kbac = go.GetComponent<KBatchedAnimController>();
            if (kbac != null) kbac.TintColour = new Color(1f, 0.30f, 0.28f, 1f);

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
