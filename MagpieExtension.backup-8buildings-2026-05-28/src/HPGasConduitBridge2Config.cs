using TUNING;
using UnityEngine;
using High_Pressure_Applications.BuildingConfigs;
using BUILDINGS = TUNING.BUILDINGS;

namespace MagpieExtension
{
    public class HPGasConduitBridge2Config : IBuildingConfig
    {
        public const string ID = "HPGasConduitBridge2";
        private static readonly HighPressureGasConduitBridgeConfig BaseConfig = new HighPressureGasConduitBridgeConfig();

        public override BuildingDef CreateBuildingDef()
        {
            BuildingDef baseDef = BaseConfig.CreateBuildingDef();

            BuildingDef def = BuildingTemplates.CreateBuildingDef(
                id:                    ID,
                width:                 4,
                height:                1,
                anim:                  "hpgas_bridge2",
                hitpoints:             30,
                construction_time:     30f,
                construction_mass:     BridgeHelpers.PipeMass,
                construction_materials: BridgeHelpers.PipeMaterials,
                melting_point:         1600f,
                build_location_rule:   BuildLocationRule.Conduit,
                decor:                 BUILDINGS.DECOR.PENALTY.TIER0,
                noise:                 NOISE_POLLUTION.NONE
            );

            def.ObjectLayer       = baseDef.ObjectLayer;
            def.SceneLayer        = baseDef.SceneLayer;
            def.InputConduitType  = baseDef.InputConduitType;
            def.OutputConduitType = baseDef.OutputConduitType;

            def.UtilityInputOffset  = new CellOffset(0, 0);
            def.UtilityOutputOffset = new CellOffset(3, 0);
            def.PermittedRotations  = PermittedRotations.FlipH;

            return def;
        }

        public override void DoPostConfigurePreview(BuildingDef def, GameObject go)
            => BaseConfig.DoPostConfigurePreview(def, go);

        public override void DoPostConfigureUnderConstruction(GameObject go)
            => BaseConfig.DoPostConfigureUnderConstruction(go);

        public override void DoPostConfigureComplete(GameObject go)
            => BaseConfig.DoPostConfigureComplete(go);
    }
}
