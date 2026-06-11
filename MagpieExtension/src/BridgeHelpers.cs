using TUNING;
using UnityEngine;
using BUILDINGS = TUNING.BUILDINGS;

namespace MagpieExtension
{
    /// <summary>Shared hardcoded build parameters that match vanilla/Ronivans bridge tiers.</summary>
    internal static class BridgeHelpers
    {
        // Logic bridge: 100 kg of refined metal
        public static float[]  LogicMass      = BUILDINGS.CONSTRUCTION_MASS_KG.TIER1;
        public static string[] LogicMaterials = MATERIALS.REFINED_METALS;

        // Solid conveyor bridge: matches vanilla SolidConduitBridgeConfig
        public static float[]  ConveyorMass      = BUILDINGS.CONSTRUCTION_MASS_KG.TIER4;
        public static string[] ConveyorMaterials = MATERIALS.ALL_METALS;

    }
}
