using System.Collections.Generic;
using TUNING;
using UnityEngine;
using BUILDINGS = TUNING.BUILDINGS;

namespace MagpieExtension
{
    public class LogicRibbonBridge3Config : IBuildingConfig
    {
        public const string ID = "LogicRibbonBridge3";
        private static readonly LogicRibbonBridgeConfig BaseConfig = new LogicRibbonBridgeConfig();

        public override BuildingDef CreateBuildingDef()
        {
            BuildingDef baseDef = BaseConfig.CreateBuildingDef();
            BuildingDef def = BuildingTemplates.CreateBuildingDef(
                ID, 5, 1, "logic_ribbon_bridge5_kanim", 30, 30f,
                BridgeHelpers.LogicMass, BridgeHelpers.LogicMaterials, 1600f,
                BuildLocationRule.LogicBridge, BUILDINGS.DECOR.PENALTY.TIER0, NOISE_POLLUTION.NONE);
            def.ViewMode = baseDef.ViewMode;
            def.ObjectLayer = baseDef.ObjectLayer;
            def.SceneLayer = baseDef.SceneLayer;
            def.Overheatable = false;
            def.Floodable = false;
            def.Entombable = false;
            def.AudioCategory = "Metal";
            def.AudioSize = "small";
            def.BaseTimeUntilRepair = -1f;
            def.PermittedRotations = PermittedRotations.R360;
            def.UtilityInputOffset = new CellOffset(0, 0);
            def.UtilityOutputOffset = new CellOffset(0, 2);
            def.AlwaysOperational = true;
            // 5-wide centered: cells [-2, -1, 0, 1, 2].
            def.LogicInputPorts = new List<LogicPorts.Port>
            {
                LogicPorts.Port.RibbonInputPort(LogicRibbonBridgeConfig.BRIDGE_LOGIC_RIBBON_IO_ID, new CellOffset(-2, 0),
                    STRINGS.BUILDINGS.PREFABS.LOGICRIBBONBRIDGE.LOGIC_PORT,
                    STRINGS.BUILDINGS.PREFABS.LOGICRIBBONBRIDGE.LOGIC_PORT_ACTIVE,
                    STRINGS.BUILDINGS.PREFABS.LOGICRIBBONBRIDGE.LOGIC_PORT_INACTIVE, false, false),
                LogicPorts.Port.RibbonInputPort(LogicRibbonBridgeConfig.BRIDGE_LOGIC_RIBBON_IO_ID, new CellOffset(2, 0),
                    STRINGS.BUILDINGS.PREFABS.LOGICRIBBONBRIDGE.LOGIC_PORT,
                    STRINGS.BUILDINGS.PREFABS.LOGICRIBBONBRIDGE.LOGIC_PORT_ACTIVE,
                    STRINGS.BUILDINGS.PREFABS.LOGICRIBBONBRIDGE.LOGIC_PORT_INACTIVE, false, false),
            };
            GeneratedBuildings.RegisterWithOverlay(OverlayModes.Logic.HighlightItemIDs, ID);
            def.AddSearchTerms(STRINGS.SEARCH_TERMS.AUTOMATION);
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
            var link = go.GetComponent<LogicUtilityNetworkLink>();
            if (link != null) { link.link1 = new CellOffset(-2, 0); link.link2 = new CellOffset(2, 0); }
            go.AddOrGet<WideLogicBridgeTinter>().isRibbon = true;
        }
    }
}
