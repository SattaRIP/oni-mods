using TUNING;
using UnityEngine;
using BUILDINGS = TUNING.BUILDINGS;

namespace MagpieExtension
{
    // Standalone 3-tile-gap (5-wide) Power Wire Bridge.
    public class WireBridge3Config : IBuildingConfig
    {
        public const string ID = "WireBridge3";
        private static readonly WireBridgeConfig BaseConfig = new WireBridgeConfig();

        public override BuildingDef CreateBuildingDef()
        {
            BuildingDef baseDef = BaseConfig.CreateBuildingDef();
            BuildingDef def = BuildingTemplates.CreateBuildingDef(
                ID, 5, 1, "utilityelectricbridge5_kanim", 30, 30f,
                baseDef.Mass, baseDef.MaterialCategory, 1600f,
                BuildLocationRule.WireBridge, BUILDINGS.DECOR.PENALTY.TIER0, NOISE_POLLUTION.NONE);
            def.ObjectLayer   = baseDef.ObjectLayer;
            def.SceneLayer    = baseDef.SceneLayer;
            def.ViewMode      = baseDef.ViewMode;
            def.AudioCategory = baseDef.AudioCategory;
            def.AudioSize     = baseDef.AudioSize;
            def.Overheatable  = false;
            def.Floodable     = false;
            def.Entombable    = false;
            def.BaseTimeUntilRepair = -1f;
            def.PermittedRotations = PermittedRotations.R360;
            // 5-wide centered: cells [-2, -1, 0, 1, 2].
            def.UtilityInputOffset  = new CellOffset(-2, 0);
            def.UtilityOutputOffset = new CellOffset(2, 0);
            GeneratedBuildings.RegisterWithOverlay(OverlayScreen.WireIDs, ID);
            def.AddSearchTerms(STRINGS.SEARCH_TERMS.POWER);
            return def;
        }

        public override void ConfigureBuildingTemplate(GameObject go, Tag prefab_tag) { BaseConfig.ConfigureBuildingTemplate(go, prefab_tag); }
        public override void DoPostConfigurePreview(BuildingDef def, GameObject go) { BaseConfig.DoPostConfigurePreview(def, go); }
        public override void DoPostConfigureUnderConstruction(GameObject go) { BaseConfig.DoPostConfigureUnderConstruction(go); }
        public override void DoPostConfigureComplete(GameObject go) { BaseConfig.DoPostConfigureComplete(go); }
    }
}
