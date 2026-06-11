using TUNING;
using UnityEngine;
using BUILDINGS = TUNING.BUILDINGS;

namespace MagpieExtensionRonivans
{
    // Wraps Ronivans' HPARailBridgeTileConfig (Heavy-Duty Joint Plate). Like
    // HPARailBridgeTile2Config, we call CreateFoundationTileDef after
    // CreateBuildingDef so the foundation-tile fields are populated; otherwise
    // placement validation fails and ONI dumps the construction material.
    public class HPARailBridgeTile3Config : IBuildingConfig
    {
        public const string ID = "HPARailBridgeTile3";
        private static readonly IBuildingConfig BaseConfig = RonivansHelpers.CreateBaseConfig(HPARailBridgeTile2Config.BASE_TYPE);

        public override BuildingDef CreateBuildingDef()
        {
            if (BaseConfig == null) return RonivansHelpers.CreateMissingDependencyDef(ID, 5, "hpa_rail_tile_bridge5_kanim");
            BuildingDef baseDef = BaseConfig.CreateBuildingDef();
            // hpa_rail_tile_bridge5_kanim: our 5-cell-wide recomposition of
            // Ronivans' art -- see HPARailBridgeTile2Config and
            // tools/gen_extended_kanims.py.
            BuildingDef def = BuildingTemplates.CreateBuildingDef(
                ID, 5, 1, "hpa_rail_tile_bridge5_kanim", 30, 30f,
                BUILDINGS.CONSTRUCTION_MASS_KG.TIER4, MATERIALS.ALL_METALS, 1600f,
                BuildLocationRule.Tile, BUILDINGS.DECOR.NONE, NOISE_POLLUTION.NONE);
            BuildingTemplates.CreateFoundationTileDef(def);
            def.TileLayer         = baseDef.TileLayer;
            def.ReplacementLayer  = baseDef.ReplacementLayer;
            def.IsFoundation      = baseDef.IsFoundation;
            def.ObjectLayer       = baseDef.ObjectLayer;
            def.SceneLayer        = baseDef.SceneLayer;
            def.ForegroundLayer   = baseDef.ForegroundLayer;
            def.InputConduitType  = baseDef.InputConduitType;
            def.OutputConduitType = baseDef.OutputConduitType;
            def.ViewMode          = baseDef.ViewMode;
            def.AudioCategory     = baseDef.AudioCategory;
            def.AudioSize         = baseDef.AudioSize;
            def.ThermalConductivity = baseDef.ThermalConductivity;
            def.UseStructureTemperature = baseDef.UseStructureTemperature;
            def.Floodable         = false;
            def.Entombable        = false;
            def.Overheatable      = false;
            def.BaseTimeUntilRepair = -1f;
            // R360 -- see HPARailBridgeTile2Config for rationale.
            def.PermittedRotations = PermittedRotations.R360;
            // 5-wide: cells span x in [-2, -1, 0, 1, 2].
            def.UtilityInputOffset  = new CellOffset(-2, 0);
            def.UtilityOutputOffset = new CellOffset(2, 0);
            GeneratedBuildings.RegisterWithOverlay(OverlayScreen.SolidConveyorIDs, ID);
            return def;
        }

        public override void ConfigureBuildingTemplate(GameObject go, Tag prefab_tag) { if (BaseConfig != null) BaseConfig.ConfigureBuildingTemplate(go, prefab_tag); }
        public override void DoPostConfigurePreview(BuildingDef def, GameObject go)
        {
            if (BaseConfig != null) BaseConfig.DoPostConfigurePreview(def, go);
        }
        public override void DoPostConfigureUnderConstruction(GameObject go)
        {
            if (BaseConfig != null) BaseConfig.DoPostConfigureUnderConstruction(go);
        }
        public override void DoPostConfigureComplete(GameObject go)
        {
            if (BaseConfig != null) BaseConfig.DoPostConfigureComplete(go);
        }
    }
}
