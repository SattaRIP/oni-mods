using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace ProtectiveWear
{
    public static class SuitInteraction
    {
        // Footwear whose worn foot art must yield to a real suit. (Garments are
        // handled at the config level -- Snazzy Swimwear is WarmVest-based, which
        // hides under suits natively; the Soft Suit retracts its own headgear.)
        // Footwear has no vanilla vest-handler, so it applies its build override
        // through the generic Equippable.GetBuildOverride path -- which we can
        // intercept below.
        public static readonly HashSet<string> FootwearArt = new HashSet<string>
        {
            "SnazzyRubberBoots", "SnazzyShoes",
        };

        // True when the dupe has an item in the SUIT slot (Atmo/Lead/Jet Suit).
        // The Soft Suit lives in the CLOTHING slot, so this is false for it.
        public static bool WearsRealSuit(GameObject dupe)
        {
            if (dupe == null) return false;
            MinionIdentity id = dupe.GetComponent<MinionIdentity>();
            if (id == null) return false;
            Equipment eq = id.GetEquipment();
            if (eq == null) return false;
            Assignable a = eq.GetAssignable(Db.Get().AssignableSlots.Suit);
            return a != null;
        }
    }

    // Drop our footwear's worn art from the render while a real suit is worn, so
    // an Atmo/Lead/Jet Suit shows cleanly instead of the boots hijacking the
    // dupe. The wearable accessorizer calls GetBuildOverride when composing the
    // dupe, and re-composes on any equipment change, so this flips back when the
    // suit comes off.
    [HarmonyPatch(typeof(Equippable), "GetBuildOverride")]
    public static class Equippable_GetBuildOverride_FootwearUnderSuit
    {
        public static void Postfix(Equippable __instance, ref KAnimFile __result)
        {
            if (__result == null || __instance == null || __instance.def == null) return;
            if (!SuitInteraction.FootwearArt.Contains(__instance.def.Id)) return;
            GameObject w = EVAHelmetManager.GetWearer(__instance);
            if (w != null && SuitInteraction.WearsRealSuit(w))
                __result = null;
        }
    }
}
