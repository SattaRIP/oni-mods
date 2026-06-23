using TUNING;
using UnityEngine;
using BUILDINGS = TUNING.BUILDINGS;

namespace MagpieExtension
{
    // Standalone 2-tile-gap (4-wide) Liquid Pipe Bridge. Own building, vanilla
    // art stretched to width; delegates behaviour to the vanilla bridge config.
    public class LiquidConduitBridge2Config : IBuildingConfig
    {
        public const string ID = "shuiguanqiao2"; // base-Magpie ID for seamless save migration
        private static readonly LiquidConduitBridgeConfig BaseConfig = new LiquidConduitBridgeConfig();

        public override BuildingDef CreateBuildingDef()
        {
            BuildingDef baseDef = BaseConfig.CreateBuildingDef();
            BuildingDef def = BuildingTemplates.CreateBuildingDef(
                ID, 4, 1, "utilityliquidbridge4_kanim", 10, 3f,
                baseDef.Mass, baseDef.MaterialCategory, 1600f,
                BuildLocationRule.Conduit, BUILDINGS.DECOR.NONE, NOISE_POLLUTION.NONE);
            def.ObjectLayer       = baseDef.ObjectLayer;
            def.SceneLayer        = baseDef.SceneLayer;
            def.InputConduitType  = ConduitType.Liquid;
            def.OutputConduitType = ConduitType.Liquid;
            def.ViewMode          = baseDef.ViewMode;
            def.AudioCategory     = baseDef.AudioCategory;
            def.AudioSize         = baseDef.AudioSize;
            def.Floodable         = false;
            def.Entombable        = false;
            def.Overheatable      = false;
            def.BaseTimeUntilRepair = -1f;
            def.PermittedRotations = PermittedRotations.R360;
            // 4-wide centered: cells [-1, 0, 1, 2]. Ports at leftmost/rightmost.
            def.UtilityInputOffset  = new CellOffset(-1, 0);
            def.UtilityOutputOffset = new CellOffset(2, 0);
            GeneratedBuildings.RegisterWithOverlay(OverlayScreen.LiquidVentIDs, ID);
            return def;
        }

        public override void ConfigureBuildingTemplate(GameObject go, Tag prefab_tag) { go.AddOrGet<ConduitBridge>().type = ConduitType.Liquid; }
        public override void DoPostConfigurePreview(BuildingDef def, GameObject go) { BaseConfig.DoPostConfigurePreview(def, go); }
        public override void DoPostConfigureUnderConstruction(GameObject go) { BaseConfig.DoPostConfigureUnderConstruction(go); }
        public override void DoPostConfigureComplete(GameObject go) { BaseConfig.DoPostConfigureComplete(go); }
    }
}
