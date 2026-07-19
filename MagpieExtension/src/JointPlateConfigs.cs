using System;
using TUNING;
using UnityEngine;
using BUILDINGS = TUNING.BUILDINGS;

namespace MagpieExtension
{
    // Wide (2/3-cell) Heavi-Watt / Conductive Joint Plates: foundation-tile
    // buildings that run heavy-watt wire through 2-3 tiles of wall, delegating
    // to the vanilla WireBridgeHighWattageConfig family the same way the wide
    // wire bridges delegate to WireBridgeConfig. The insulated 3-wide variants
    // pair with the "Insulated Joint Plate [FIXED]" Workshop mod (3434920353),
    // which already ships 1- and 2-wide insulated plates but no 3-wide; when
    // that mod is absent they register Deprecated (hidden), matching the
    // MagpieExtensionRonivans soft-dependency pattern.
    internal static class JointPlateHelpers
    {
        // Type from the Insulated Joint Plate mod's assembly; presence = active.
        private const string PROBE_TYPE = "InsulatingPlate";
        private static bool? insulatedModLoaded;

        public static bool InsulatedModLoaded
        {
            get
            {
                if (!insulatedModLoaded.HasValue)
                {
                    insulatedModLoaded = false;
                    foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
                    {
                        if (asm.GetName().Name == "InsulatedJointPlate" || asm.GetType(PROBE_TYPE) != null)
                        {
                            insulatedModLoaded = true;
                            break;
                        }
                    }
                }
                return insulatedModLoaded.Value;
            }
        }

        public static BuildingDef CreateDef(IBuildingConfig baseConfig, string id, int width, string anim, bool insulated)
        {
            BuildingDef baseDef = baseConfig.CreateBuildingDef();
            BuildingDef def = BuildingTemplates.CreateBuildingDef(
                id, width, 1, anim, 100, 3f,
                baseDef.Mass, baseDef.MaterialCategory, 1600f,
                baseDef.BuildLocationRule, BUILDINGS.DECOR.PENALTY.TIER0, NOISE_POLLUTION.NONE);
            BuildingTemplates.CreateFoundationTileDef(def);
            def.Overheatable            = false;
            def.UseStructureTemperature = false;
            def.Floodable               = false;
            def.Entombable              = false;
            def.ViewMode        = baseDef.ViewMode;
            def.AudioCategory   = baseDef.AudioCategory;
            def.AudioSize       = baseDef.AudioSize;
            def.ObjectLayer     = baseDef.ObjectLayer;
            def.SceneLayer      = baseDef.SceneLayer;
            def.ForegroundLayer = baseDef.ForegroundLayer;
            def.BaseTimeUntilRepair = -1f;
            def.PermittedRotations  = baseDef.PermittedRotations;
            // Wide centered footprints: 2-wide occupies [0,1], 3-wide [-1..1];
            // the wire connects one cell beyond each end, like the wide bridges.
            def.UtilityInputOffset  = Input(width);
            def.UtilityOutputOffset = Output(width);
            if (insulated)
            {
                // Same mechanism the Insulated Joint Plate mod uses: near-zero
                // building conductivity, applied to the cells on spawn.
                def.ThermalConductivity = 0.01f;
                if (!InsulatedModLoaded)
                    def.Deprecated = true;
            }
            GeneratedBuildings.RegisterWithOverlay(OverlayScreen.WireIDs, id);
            return def;
        }

        public static CellOffset Input(int width)  { return width == 2 ? new CellOffset(-1, 0) : new CellOffset(-2, 0); }
        public static CellOffset Output(int width) { return new CellOffset(2, 0); }
    }

    // Applies/clears cell insulation for the insulated plates (our own tiny
    // equivalent of the Insulated Joint Plate mod's InsulatingPlate component;
    // theirs never clears on deconstruct, ours does).
    public class JointPlateInsulation : KMonoBehaviour
    {
        protected override void OnSpawn()
        {
            base.OnSpawn();
            var building = GetComponent<Building>();
            if (building == null) return;
            float value = building.Def != null ? building.Def.ThermalConductivity : 0.01f;
            foreach (int cell in building.PlacementCells)
                SimMessages.SetInsulation(cell, value);
        }

        protected override void OnCleanUp()
        {
            var building = GetComponent<Building>();
            if (building != null)
            {
                foreach (int cell in building.PlacementCells)
                    SimMessages.SetInsulation(cell, 1f);
            }
            base.OnCleanUp();
        }
    }

    public abstract class JointPlateConfigBase : IBuildingConfig
    {
        protected abstract IBuildingConfig BaseConfig { get; }
        protected abstract string Id { get; }
        protected abstract int Width { get; }
        protected abstract string Anim { get; }
        protected virtual bool Insulated { get { return false; } }

        public override BuildingDef CreateBuildingDef()
        {
            return JointPlateHelpers.CreateDef(BaseConfig, Id, Width, Anim, Insulated);
        }

        public override void ConfigureBuildingTemplate(GameObject go, Tag prefab_tag)
        {
            BaseConfig.ConfigureBuildingTemplate(go, prefab_tag);
        }

        public override void DoPostConfigurePreview(BuildingDef def, GameObject go)
        {
            BaseConfig.DoPostConfigurePreview(def, go);
            BridgeLink.Repoint(go, JointPlateHelpers.Input(Width), JointPlateHelpers.Output(Width));
        }

        public override void DoPostConfigureUnderConstruction(GameObject go)
        {
            BaseConfig.DoPostConfigureUnderConstruction(go);
            BridgeLink.Repoint(go, JointPlateHelpers.Input(Width), JointPlateHelpers.Output(Width));
        }

        public override void DoPostConfigureComplete(GameObject go)
        {
            BaseConfig.DoPostConfigureComplete(go);
            BridgeLink.Repoint(go, JointPlateHelpers.Input(Width), JointPlateHelpers.Output(Width));
            if (Insulated)
                go.AddOrGet<JointPlateInsulation>();
        }
    }

    public class WireBridgeHighWattage2Config : JointPlateConfigBase
    {
        public const string ID = "WireBridgeHighWattage2";
        private static readonly IBuildingConfig Base = new WireBridgeHighWattageConfig();
        protected override IBuildingConfig BaseConfig { get { return Base; } }
        protected override string Id { get { return ID; } }
        protected override int Width { get { return 2; } }
        protected override string Anim { get { return "heavywatttile2_kanim"; } }
    }

    public class WireBridgeHighWattage3Config : JointPlateConfigBase
    {
        public const string ID = "WireBridgeHighWattage3";
        private static readonly IBuildingConfig Base = new WireBridgeHighWattageConfig();
        protected override IBuildingConfig BaseConfig { get { return Base; } }
        protected override string Id { get { return ID; } }
        protected override int Width { get { return 3; } }
        protected override string Anim { get { return "heavywatttile3_kanim"; } }
    }

    public class WireRefinedBridgeHighWattage2Config : JointPlateConfigBase
    {
        public const string ID = "WireRefinedBridgeHighWattage2";
        private static readonly IBuildingConfig Base = new WireRefinedBridgeHighWattageConfig();
        protected override IBuildingConfig BaseConfig { get { return Base; } }
        protected override string Id { get { return ID; } }
        protected override int Width { get { return 2; } }
        protected override string Anim { get { return "heavywatttile_conductive2_kanim"; } }
    }

    public class WireRefinedBridgeHighWattage3Config : JointPlateConfigBase
    {
        public const string ID = "WireRefinedBridgeHighWattage3";
        private static readonly IBuildingConfig Base = new WireRefinedBridgeHighWattageConfig();
        protected override IBuildingConfig BaseConfig { get { return Base; } }
        protected override string Id { get { return ID; } }
        protected override int Width { get { return 3; } }
        protected override string Anim { get { return "heavywatttile_conductive3_kanim"; } }
    }

    public class InsulatedWireBridgeHighWattage3Config : JointPlateConfigBase
    {
        public const string ID = "InsulatedWireBridgeHighWattage3";
        private static readonly IBuildingConfig Base = new WireBridgeHighWattageConfig();
        protected override IBuildingConfig BaseConfig { get { return Base; } }
        protected override string Id { get { return ID; } }
        protected override int Width { get { return 3; } }
        protected override string Anim { get { return "heavywatttile_ins3_kanim"; } }
        protected override bool Insulated { get { return true; } }
    }

    public class InsulatedWireRefinedBridgeHighWattage3Config : JointPlateConfigBase
    {
        public const string ID = "InsulatedWireRefinedBridgeHighWattage3";
        private static readonly IBuildingConfig Base = new WireRefinedBridgeHighWattageConfig();
        protected override IBuildingConfig BaseConfig { get { return Base; } }
        protected override string Id { get { return ID; } }
        protected override int Width { get { return 3; } }
        protected override string Anim { get { return "heavywatttile_conductive_ins3_kanim"; } }
        protected override bool Insulated { get { return true; } }
    }
}
