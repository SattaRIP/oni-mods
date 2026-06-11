using TUNING;
using UnityEngine;
using BUILDINGS = TUNING.BUILDINGS;

namespace MagpieExtension
{
    public class LogicWireBridge3Config : IBuildingConfig
    {
        public const string ID = "LogicWireBridge3";
        private static readonly LogicWireBridgeConfig BaseConfig = new LogicWireBridgeConfig();

        public override BuildingDef CreateBuildingDef()
        {
            BuildingDef baseDef = BaseConfig.CreateBuildingDef();
            BuildingDef def = BuildingTemplates.CreateBuildingDef(
                ID, 5, 1, "logicwire_bridge3", 30, 30f,
                BridgeHelpers.LogicMass, BridgeHelpers.LogicMaterials, 1600f,
                BuildLocationRule.Tile, BUILDINGS.DECOR.PENALTY.TIER0, NOISE_POLLUTION.NONE);
            def.ObjectLayer = baseDef.ObjectLayer; def.SceneLayer = baseDef.SceneLayer;
            def.UtilityInputOffset = new CellOffset(0, 0); def.UtilityOutputOffset = new CellOffset(4, 0);
            def.PermittedRotations = PermittedRotations.FlipH;
            return def;
        }
        public override void DoPostConfigurePreview(BuildingDef def, GameObject go) => BaseConfig.DoPostConfigurePreview(def, go);
        public override void DoPostConfigureUnderConstruction(GameObject go) => BaseConfig.DoPostConfigureUnderConstruction(go);
        public override void DoPostConfigureComplete(GameObject go)
        {
            BaseConfig.DoPostConfigureComplete(go);
            var link = go.GetComponent<LogicUtilityNetworkLink>();
            if (link != null) { link.link1 = new CellOffset(0, 0); link.link2 = new CellOffset(4, 0); }
        }
    }
}
