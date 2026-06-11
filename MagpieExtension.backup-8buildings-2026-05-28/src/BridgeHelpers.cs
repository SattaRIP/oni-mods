using TUNING;
using BUILDINGS = TUNING.BUILDINGS;

namespace MagpieExtension
{
    /// <summary>Shared hardcoded build parameters that match vanilla/Ronivans bridge tiers.</summary>
    internal static class BridgeHelpers
    {
        // Standard pipe bridge: 100 kg of any metal
        public static float[]  PipeMass      = BUILDINGS.CONSTRUCTION_MASS_KG.TIER1;
        public static string[] PipeMaterials = MATERIALS.ALL_METALS;

        // Logic bridge: 100 kg of refined metal
        public static float[]  LogicMass      = BUILDINGS.CONSTRUCTION_MASS_KG.TIER1;
        public static string[] LogicMaterials = MATERIALS.REFINED_METALS;
    }
}
