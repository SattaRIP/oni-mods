using TUNING;
using UnityEngine;
using BUILDINGS = TUNING.BUILDINGS;

namespace MagpieExtension
{
    // Standalone 2-tile-gap (4-wide) Insulated Conductive Wire Bridge.
    public class WireRubberBridge2Config : IBuildingConfig
    {
        public const string ID = "WireRubberBridge2";
        private static readonly WireRubberBridgeConfig BaseConfig = new WireRubberBridgeConfig();

        public override BuildingDef CreateBuildingDef()
        {
            BuildingDef baseDef = BaseConfig.CreateBuildingDef();
            BuildingDef def = BuildingTemplates.CreateBuildingDef(
                ID, 4, 1, "utilityelectricbridgerubber4_kanim", 30, 3f,
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
            // 4-wide centered: cells [-1, 0, 1, 2].
            def.UtilityInputOffset  = new CellOffset(-1, 0);
            def.UtilityOutputOffset = new CellOffset(2, 0);
            GeneratedBuildings.RegisterWithOverlay(OverlayScreen.WireIDs, ID);
            def.AddSearchTerms(STRINGS.SEARCH_TERMS.POWER);
            return def;
        }

        public override void ConfigureBuildingTemplate(GameObject go, Tag prefab_tag) { BaseConfig.ConfigureBuildingTemplate(go, prefab_tag); }
        public override void DoPostConfigurePreview(BuildingDef def, GameObject go)
        {
            BaseConfig.DoPostConfigurePreview(def, go);
            // The base adds the +/-1 link here too; without this the placement
            // ghost draws its sockets one tile in from the terminals.
            BridgeLink.Repoint(go, new CellOffset(-1, 0), new CellOffset(2, 0));
        }
        public override void DoPostConfigureUnderConstruction(GameObject go)
        {
            BaseConfig.DoPostConfigureUnderConstruction(go);
            BridgeLink.Repoint(go, new CellOffset(-1, 0), new CellOffset(2, 0));
        }
        public override void DoPostConfigureComplete(GameObject go)
        {
            BaseConfig.DoPostConfigureComplete(go);
            // Base hardcodes the link at +/-1; re-point it to this bridge's real ends.
            BridgeLink.Repoint(go, new CellOffset(-1, 0), new CellOffset(2, 0));
        }
    }
}
