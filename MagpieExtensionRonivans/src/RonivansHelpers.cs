using System;
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
    /// History: this file used to also carry StretchKanim / DuplicateKanimAcrossCells
    /// runtime-visual hacks for the wide bridge/joint-plate variants. Those were
    /// replaced by real generated kanim assets (anim/magpie_extended_anims/,
    /// produced by tools/gen_extended_kanims.py), which ONI renders and rotates
    /// natively.
    /// </summary>
    internal static class RonivansHelpers
    {
        public static IBuildingConfig CreateBaseConfig(string fullyQualifiedTypeName)
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                var t = asm.GetType(fullyQualifiedTypeName);
                if (t != null)
                {
                    try
                    {
                        return (IBuildingConfig)Activator.CreateInstance(t);
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError("[MagpieExtensionRonivans] Failed to instantiate "
                                       + fullyQualifiedTypeName + ": " + ex);
                        return null;
                    }
                }
            }
            Debug.LogError("[MagpieExtensionRonivans] Required Ronivans type not found: "
                           + fullyQualifiedTypeName + ". Make sure Ronivans Legacy is installed and enabled.");
            return null;
        }
    }
}
