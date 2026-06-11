using TUNING;
using UnityEngine;
using BUILDINGS = TUNING.BUILDINGS;

namespace MagpieExtension
{
    public class SolidConduitBridge2Config : IBuildingConfig
    {
        public const string ID = "SolidConduitBridge2";
        private static readonly SolidConduitBridgeConfig BaseConfig = new SolidConduitBridgeConfig();

        public override BuildingDef CreateBuildingDef()
        {
            BuildingDef baseDef = BaseConfig.CreateBuildingDef();
            BuildingDef def = BuildingTemplates.CreateBuildingDef(
                ID, 4, 1, "utilities_conveyorbridge4_kanim", 30, 30f,
                BridgeHelpers.ConveyorMass, BridgeHelpers.ConveyorMaterials, 1600f,
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
            // 4-wide centered: cells [-1, 0, 1, 2].
            def.UtilityInputOffset  = new CellOffset(-1, 0);
            def.UtilityOutputOffset = new CellOffset(2, 0);
            GeneratedBuildings.RegisterWithOverlay(OverlayScreen.SolidConveyorIDs, ID);
            return def;
        }

        public override void DoPostConfigurePreview(BuildingDef def, GameObject go)
        {
            BaseConfig.DoPostConfigurePreview(def, go);
        }
        public override void DoPostConfigureUnderConstruction(GameObject go)
        {
            BaseConfig.DoPostConfigureUnderConstruction(go);
        }
        public override void DoPostConfigureComplete(GameObject go)
        {
            BaseConfig.DoPostConfigureComplete(go);
        }
    }
}
