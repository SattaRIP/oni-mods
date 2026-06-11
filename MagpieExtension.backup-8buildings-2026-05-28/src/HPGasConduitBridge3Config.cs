using TUNING;
using UnityEngine;
using High_Pressure_Applications.BuildingConfigs;
using BUILDINGS = TUNING.BUILDINGS;

namespace MagpieExtension
{
    public class HPGasConduitBridge3Config : IBuildingConfig
    {
        public const string ID = "HPGasConduitBridge3";
        private static readonly HighPressureGasConduitBridgeConfig BaseConfig = new HighPressureGasConduitBridgeConfig();

        public override BuildingDef CreateBuildingDef()
        {
            BuildingDef baseDef = BaseConfig.CreateBuildingDef();
            BuildingDef def = BuildingTemplates.CreateBuildingDef(
                ID, 5, 1, "hpgas_bridge3", 30, 30f,
                BridgeHelpers.PipeMass, BridgeHelpers.PipeMaterials, 1600f,
                BuildLocationRule.Conduit, BUILDINGS.DECOR.PENALTY.TIER0, NOISE_POLLUTION.NONE);
            def.ObjectLayer = baseDef.ObjectLayer; def.SceneLayer = baseDef.SceneLayer;
            def.InputConduitType = baseDef.InputConduitType; def.OutputConduitType = baseDef.OutputConduitType;
            def.UtilityInputOffset = new CellOffset(0, 0); def.UtilityOutputOffset = new CellOffset(4, 0);
            def.PermittedRotations = PermittedRotations.FlipH;
            return def;
        }
        public override void DoPostConfigurePreview(BuildingDef def, GameObject go) => BaseConfig.DoPostConfigurePreview(def, go);
        public override void DoPostConfigureUnderConstruction(GameObject go) => BaseConfig.DoPostConfigureUnderConstruction(go);
        public override void DoPostConfigureComplete(GameObject go) => BaseConfig.DoPostConfigureComplete(go);
    }
}
