using System;
using TUNING;
using UnityEngine;

namespace MagpieExtensionRonivans
{
    /// <summary>
    /// Helpers for instantiating Ronivans' internal IBuildingConfig classes via
    /// reflection. The configs we wrap are <c>internal</c> in Ronivans' assembly,
    /// so we can't reference their types at compile time -- instead we look them
    /// up by name at first use, cast to the public IBuildingConfig interface, and
    /// delegate through that.
    ///
    /// Ronivans' assembly can legitimately be absent for one boot: when Steam
    /// updates the mod, ONI logs "Latent reinstall of mod ..." and skips loading
    /// its DLL while reinstalling (seen on the U59 DLC update, 2026-06-11). On
    /// such a boot everything here must fail SOFT: BuildingConfigManager
    /// .RegisterBuilding dereferences whatever CreateBuildingDef returns, so a
    /// null def NREs, and any Debug.LogError during load brings up ONI's crash
    /// screen. Hence LogWarning + placeholder defs, never LogError + null.
    ///
    /// History: this file used to also carry StretchKanim / DuplicateKanimAcrossCells
    /// runtime-visual hacks for the wide bridge/joint-plate variants. Those were
    /// replaced by real generated kanim assets (anim/magpie_extended_anims/,
    /// produced by tools/gen_extended_kanims.py), which ONI renders and rotates
    /// natively.
    /// </summary>
    internal static class RonivansHelpers
    {
        // Probe class: present in every Ronivans version we've targeted.
        private const string PROBE_TYPE =
            "RonivansLegacy_ChemicalProcessing.Content.Defs.Buildings.DupesLogistics.LogisticBridgeConfig";

        private static bool? ronivansLoaded;

        /// <summary>True when Ronivans Legacy's assembly is loaded this boot.</summary>
        public static bool RonivansLoaded
        {
            get
            {
                if (!ronivansLoaded.HasValue)
                    ronivansLoaded = FindType(PROBE_TYPE) != null;
                return ronivansLoaded.Value;
            }
        }

        public static IBuildingConfig CreateBaseConfig(string fullyQualifiedTypeName)
        {
            var t = FindType(fullyQualifiedTypeName);
            if (t == null)
            {
                Debug.LogWarning("[MagpieExtensionRonivans] Ronivans type not found: "
                                 + fullyQualifiedTypeName + ". Ronivans Legacy is missing or mid-reinstall;"
                                 + " the wide bridge variants will sit out this boot.");
                return null;
            }
            try
            {
                return (IBuildingConfig)Activator.CreateInstance(t);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[MagpieExtensionRonivans] Failed to instantiate "
                                 + fullyQualifiedTypeName + ": " + ex);
                return null;
            }
        }

        /// <summary>
        /// Stand-in def registered when the Ronivans base config is unavailable.
        /// RegisterBuilding requires a non-null def; Deprecated keeps it out of
        /// the build menu and codex, so the boot proceeds without the variants.
        /// </summary>
        public static BuildingDef CreateMissingDependencyDef(string id, int width, string anim)
        {
            BuildingDef def = BuildingTemplates.CreateBuildingDef(
                id, width, 1, anim, 30, 30f,
                BUILDINGS.CONSTRUCTION_MASS_KG.TIER4, MATERIALS.ALL_METALS, 1600f,
                BuildLocationRule.Anywhere, BUILDINGS.DECOR.NONE, NOISE_POLLUTION.NONE);
            def.Deprecated = true;
            return def;
        }

        private static Type FindType(string fullyQualifiedTypeName)
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                var t = asm.GetType(fullyQualifiedTypeName);
                if (t != null) return t;
            }
            return null;
        }
    }
}
