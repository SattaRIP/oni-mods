using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace ProtectiveWear
{
    public static class SuitInteraction
    {
        // Our garments/footwear that carry worn-body art. When a real suit is
        // worn on top, this art must yield so the suit renders instead of our
        // clothing poking through the symbols the suit body doesn't cover
        // (e.g. foot, belt).
        public static readonly HashSet<string> WornArtItems = new HashSet<string>
        {
            "SnazzySwimwear", "SnazzyRubberBoots", "SnazzyShoes", "EVASuit", "UpgradedWarmCoat",
        };

        // True when the dupe has an item in the SUIT slot (Atmo/Lead/Jet Suit).
        // The Soft Suit lives in the CLOTHING slot, so this is false for it --
        // exactly what we want, so a Soft-Suit-only dupe isn't treated as suited.
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

    // Hide our worn garment/footwear art while a real suit is worn. The Atmo
    // Suit body build (body_oxygen) covers most symbols at a higher priority (6)
    // than clothing (4), but it has NO foot/belt symbol -- so our full-body
    // recolours left gold bits poking through the suit's feet/waist, and the
    // suit read as "not showing". Returning a null build override for our items
    // while suited drops them from the composite, so the suit renders clean. The
    // game re-queries this whenever equipment changes, so it flips back when the
    // suit comes off.
    [HarmonyPatch(typeof(Equippable), "GetBuildOverride")]
    public static class Equippable_GetBuildOverride_HideUnderSuit
    {
        public static void Postfix(Equippable __instance, ref KAnimFile __result)
        {
            if (__result == null || __instance == null || __instance.def == null) return;
            if (!SuitInteraction.WornArtItems.Contains(__instance.def.Id)) return;
            GameObject w = EVAHelmetManager.GetWearer(__instance);
            if (w != null && SuitInteraction.WearsRealSuit(w))
                __result = null;
        }
    }
}
