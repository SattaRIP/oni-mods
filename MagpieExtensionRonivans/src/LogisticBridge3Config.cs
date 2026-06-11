using TUNING;
using UnityEngine;
using BUILDINGS = TUNING.BUILDINGS;

namespace MagpieExtensionRonivans
{
    public class LogisticBridge3Config : IBuildingConfig
    {
        public const string ID = "LogisticBridge3";
        private static readonly IBuildingConfig BaseConfig = RonivansHelpers.CreateBaseConfig(LogisticBridge2Config.BASE_TYPE);

        public override BuildingDef CreateBuildingDef()
        {
            if (BaseConfig == null) return RonivansHelpers.CreateMissingDependencyDef(ID, 5, "logistic_bridge5_kanim");
            BuildingDef baseDef = BaseConfig.CreateBuildingDef();
            BuildingDef def = BuildingTemplates.CreateBuildingDef(
                ID, 5, 1, "logistic_bridge5_kanim", 30, 30f,
                BUILDINGS.CONSTRUCTION_MASS_KG.TIER4, MATERIALS.ALL_METALS, 1600f,
                BuildLocationRule.Conduit, BUILDINGS.DECOR.NONE, NOISE_POLLUTION.NONE);
            def.ObjectLayer       = baseDef.ObjectLayer;
            def.SceneLayer        = baseDef.SceneLayer;
            def.InputConduitType  = ConduitType.Solid;
            def.OutputConduitType = ConduitType.Solid;
            def.ViewMode          = baseDef.ViewMode;
            def.AudioCategory     = baseDef.AudioCategory;
            def.AudioSize         = baseDef.AudioSize;
            def.Floodable         = false;
            def.Entombable        = false;
            def.Overheatable      = false;
            def.BaseTimeUntilRepair = -1f;
            def.PermittedRotations = PermittedRotations.R360;
            // 5-wide centered: cells [-2, -1, 0, 1, 2].
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
