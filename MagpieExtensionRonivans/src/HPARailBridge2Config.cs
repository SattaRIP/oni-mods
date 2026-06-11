using TUNING;
using UnityEngine;
using BUILDINGS = TUNING.BUILDINGS;

namespace MagpieExtensionRonivans
{
    public class HPARailBridge2Config : IBuildingConfig
    {
        public const string ID = "HPARailBridge2";
        public const string BASE_TYPE = "RonivansLegacy_ChemicalProcessing.Content.Defs.Buildings.HighPressureApplications.HighCapacityLogisticRails.HPARailBridgeConfig";
        private static readonly IBuildingConfig BaseConfig = RonivansHelpers.CreateBaseConfig(BASE_TYPE);

        public override BuildingDef CreateBuildingDef()
        {
            if (BaseConfig == null) return RonivansHelpers.CreateMissingDependencyDef(ID, 4, "hpa_rail_bridge4_kanim");
            BuildingDef baseDef = BaseConfig.CreateBuildingDef();
            BuildingDef def = BuildingTemplates.CreateBuildingDef(
                ID, 4, 1, "hpa_rail_bridge4_kanim", 30, 30f,
                BUILDINGS.CONSTRUCTION_MASS_KG.TIER4, MATERIALS.ALL_METALS, 1600f,
                BuildLocationRule.Conduit, BUILDINGS.DECOR.NONE, NOISE_POLLUTION.NONE);
            def.ObjectLayer       = baseDef.ObjectLayer;
            def.SceneLayer        = baseDef.SceneLayer;
            def.InputConduitType  = baseDef.InputConduitType;
            def.OutputConduitType = baseDef.OutputConduitType;
            def.ViewMode          = baseDef.ViewMode;
            def.AudioCategory     = baseDef.AudioCategory;
            def.AudioSize         = baseDef.AudioSize;
            def.Floodable         = false;
            def.Entombable        = false;
            def.Overheatable      = false;
            def.BaseTimeUntilRepair = -1f;
            def.PermittedRotations = PermittedRotations.R360;
            // 4-wide centered: cells [-1, 0, 1, 2].
            def.UtilityInputOffset  = new CellOffset(-1, 0);
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
