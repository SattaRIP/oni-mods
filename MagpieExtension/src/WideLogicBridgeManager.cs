using System.Collections.Generic;
using UnityEngine;

namespace MagpieExtension
{
    // Drives automation-overlay signal tinting for the wide logic bridges WITHOUT a
    // per-building KMonoBehaviour. Adding such a component crashed spawn with Mono
    // "Method has zero rva", so all wiring lives in plain Harmony postfixes (see
    // LogicTintPatches) feeding this single static registry.
    //
    // Why this is needed: vanilla OverlayModes.Logic.Update only tints anims whose
    // KPrefabID.PrefabTag is EXACTLY "LogicWireBridge"/"LogicRibbonBridge", so our
    // LogicWireBridge2/3 and LogicRibbonBridge2/3 never get coloured and stay blue.
    // We replicate vanilla's colouring locally:
    //   - wire bridge:   whole-anim TintColour = logicOn/logicOff by bit 0 (alpha 0)
    //   - ribbon bridge: per-symbol SetSymbolTint on wire1..wire4 by bits 0..3
    // Colours come from GlobalAssets.Instance.colorSet so accessibility swaps work.
    internal static class WideLogicBridgeManager
    {
        private struct Entry
        {
            public KBatchedAnimController controller;
            public LogicUtilityNetworkLink link;
            public bool isRibbon;
        }

        private static readonly KAnimHashedString[] WireSymbols =
        {
            new KAnimHashedString("wire1"), new KAnimHashedString("wire2"),
            new KAnimHashedString("wire3"), new KAnimHashedString("wire4"),
        };

        // Our four wide-logic-bridge prefab tags -> isRibbon.
        private static readonly Dictionary<Tag, bool> OurTags = new Dictionary<Tag, bool>();
        private static readonly Dictionary<GameObject, Entry> Entries = new Dictionary<GameObject, Entry>();
        private static bool tinted;

        private static void EnsureTags()
        {
            if (OurTags.Count > 0) return;
            OurTags[TagManager.Create(LogicWireBridge2Config.ID)]   = false;
            OurTags[TagManager.Create(LogicWireBridge3Config.ID)]   = false;
            OurTags[TagManager.Create(LogicRibbonBridge2Config.ID)] = true;
            OurTags[TagManager.Create(LogicRibbonBridge3Config.ID)] = true;
        }

        // Called from a Building.OnSpawn postfix for every building; cheap tag check.
        public static void TryRegister(GameObject go)
        {
            if (go == null) return;
            EnsureTags();
            KPrefabID kpid = go.GetComponent<KPrefabID>();
            if (kpid == null) return;
            bool isRibbon;
            if (!OurTags.TryGetValue(kpid.PrefabTag, out isRibbon)) return;
            KBatchedAnimController controller = go.GetComponent<KBatchedAnimController>();
            LogicUtilityNetworkLink link = go.GetComponent<LogicUtilityNetworkLink>();
            if (controller == null || link == null) return;
            Entry entry;
            entry.controller = controller;
            entry.link = link;
            entry.isRibbon = isRibbon;
            Entries[go] = entry;
        }

        public static void Unregister(GameObject go)
        {
            if (go != null) Entries.Remove(go);
        }

        // Called from an OverlayModes.Logic.Update postfix (runs only while the logic
        // overlay is the active mode).
        public static void ApplyTints()
        {
            if (Entries.Count == 0) return;
            LogicCircuitManager manager = Game.Instance != null ? Game.Instance.logicCircuitManager : null;
            if (manager == null || GlobalAssets.Instance == null) return;

            Color32 on = GlobalAssets.Instance.colorSet.logicOn;
            Color32 off = GlobalAssets.Instance.colorSet.logicOff;
            on.a = 0;
            off.a = 0;

            foreach (Entry entry in Entries.Values)
            {
                if (entry.controller == null || entry.link == null) continue;
                LogicCircuitNetwork network = manager.GetNetworkForCell(entry.link.GetNetworkCell());
                if (!entry.isRibbon)
                {
                    entry.controller.TintColour = (network != null && network.IsBitActive(0)) ? on : off;
                }
                else
                {
                    for (int bit = 0; bit < WireSymbols.Length; bit++)
                    {
                        entry.controller.SetSymbolTint(WireSymbols[bit],
                            (network != null && network.IsBitActive(bit)) ? (Color)on : (Color)off);
                    }
                }
            }
            tinted = true;
        }

        // Called from an OverlayModes.Logic.Disable postfix when the overlay closes.
        public static void ResetTints()
        {
            if (!tinted) return;
            foreach (Entry entry in Entries.Values)
            {
                if (entry.controller == null) continue;
                entry.controller.TintColour = Color.white;
                if (entry.isRibbon)
                {
                    foreach (KAnimHashedString sym in WireSymbols)
                        entry.controller.SetSymbolTint(sym, Color.white);
                }
            }
            tinted = false;
        }
    }
}
