using TUNING;
using UnityEngine;
using BUILDINGS = TUNING.BUILDINGS;

namespace MagpieExtension
{
    // Standalone 3-tile-gap (5-wide) Gas Pipe Bridge.
    public class GasConduitBridge3Config : IBuildingConfig
    {
        public const string ID = "qiguanqiao3"; // base-Magpie ID for seamless save migration
        private static readonly GasConduitBridgeConfig BaseConfig = new GasConduitBridgeConfig();

        public override BuildingDef CreateBuildingDef()
        {
            BuildingDef baseDef = BaseConfig.CreateBuildingDef();
            BuildingDef def = BuildingTemplates.CreateBuildingDef(
                ID, 5, 1, "utilitygasbridge5_kanim", 10, 3f,
                baseDef.Mass, baseDef.MaterialCategory, 1600f,
                BuildLocationRule.Conduit, BUILDINGS.DECOR.NONE, NOISE_POLLUTION.NONE);
            def.ObjectLayer       = baseDef.ObjectLayer;
            def.SceneLayer        = baseDef.SceneLayer;
            def.InputConduitType  = ConduitType.Gas;
            def.OutputConduitType = ConduitType.Gas;
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
            GeneratedBuildings.RegisterWithOverlay(OverlayScreen.GasVentIDs, ID);
            return def;
        }

        public override void ConfigureBuildingTemplate(GameObject go, Tag prefab_tag) { go.AddOrGet<ConduitBridge>().type = ConduitType.Gas; }
        public override void DoPostConfigurePreview(BuildingDef def, GameObject go) { BaseConfig.DoPostConfigurePreview(def, go); }
        public override void DoPostConfigureUnderConstruction(GameObject go) { BaseConfig.DoPostConfigureUnderConstruction(go); }
        public override void DoPostConfigureComplete(GameObject go) { BaseConfig.DoPostConfigureComplete(go); }
    }
}
