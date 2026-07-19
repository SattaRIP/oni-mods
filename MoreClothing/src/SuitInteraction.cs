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

        // Worn-art build overrides we're tracking, keyed by wearer, plus their
        // priority so we can restore them. Vests apply their build through the
        // ClothingWearer path (not GetBuildOverride), and the game doesn't drop
        // ours when a suit goes on -- so we pull it off the dupe's controller
        // directly while suited and add it back afterwards.
        private static readonly Dictionary<GameObject, Dictionary<KAnimFile, int>> tracked =
            new Dictionary<GameObject, Dictionary<KAnimFile, int>>();
        private static readonly HashSet<GameObject> pulledWearers = new HashSet<GameObject>();
        private static float timer;

        public static void Register(Equippable eq)
        {
            GameObject w = EVAHelmetManager.GetWearer(eq);
            KAnimFile k = eq != null ? eq.GetBuildOverride() : null;
            if (w == null || k == null) return;
            int prio = eq.def != null ? eq.def.BuildOverridePriority : 4;
            if (!tracked.TryGetValue(w, out var m)) { m = new Dictionary<KAnimFile, int>(); tracked[w] = m; }
            m[k] = prio;
        }

        public static void Unregister(Equippable eq)
        {
            GameObject w = EVAHelmetManager.GetWearer(eq);
            KAnimFile k = eq != null ? eq.GetBuildOverride() : null;
            if (w == null || k == null) return;
            if (tracked.TryGetValue(w, out var m))
            {
                m.Remove(k);
                if (m.Count == 0) { tracked.Remove(w); pulledWearers.Remove(w); }
            }
        }

        public static void Tick(float dt)
        {
            if (tracked.Count == 0) return;
            timer += dt;
            if (timer < 0.2f) return;
            timer = 0f;
            foreach (var kv in new List<KeyValuePair<GameObject, Dictionary<KAnimFile, int>>>(tracked))
            {
                GameObject w = kv.Key;
                if (w == null) { tracked.Remove(w); pulledWearers.Remove(w); continue; }
                SymbolOverrideController soc = w.GetComponent<SymbolOverrideController>();
                if (soc == null) continue;
                bool suited = WearsRealSuit(w);
                bool isPulled = pulledWearers.Contains(w);
                if (suited && !isPulled)
                {
                    foreach (var pair in kv.Value)
                        SymbolOverrideControllerUtil.TryRemoveBuildOverride(soc, pair.Key.GetData(), pair.Value);
                    pulledWearers.Add(w);
                    soc.ApplyOverrides();
                }
                else if (!suited && isPulled)
                {
                    foreach (var pair in kv.Value)
                        SymbolOverrideControllerUtil.AddBuildOverride(soc, pair.Key.GetData(), pair.Value);
                    pulledWearers.Remove(w);
                    soc.ApplyOverrides();
                }
            }
        }
    }

}
