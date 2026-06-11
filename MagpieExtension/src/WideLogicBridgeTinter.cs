using UnityEngine;

namespace MagpieExtension
{
    // Signal-state tinting for the wide logic bridges in the automation overlay.
    //
    // Vanilla's OverlayModes.Logic.Update() collects bridge anims by EXACT
    // prefab tag ("LogicWireBridge" / "LogicRibbonBridge"), so our
    // LogicWireBridge2/3 and LogicRibbonBridge2/3 prefabs are never tinted and
    // stay blue in the overlay. Patching that method is off the table (it's the
    // Mono dynamic-method crash family — see PlacementGuard notes), so this
    // component replicates the vanilla colouring locally:
    //   - wire bridge: whole-anim TintColour = logicOn/logicOff by bit 0 of the
    //     network at the link cell (alpha forced to 0, as vanilla does)
    //   - ribbon bridge: per-symbol SetSymbolTint on wire1..wire4 by bits 0..3
    // Colours come from GlobalAssets.Instance.colorSet, so accessibility colour
    // swaps keep working.
    public class WideLogicBridgeTinter : KMonoBehaviour
    {
        public bool isRibbon;

        private static readonly KAnimHashedString[] WireSymbols =
        {
            new KAnimHashedString("wire1"), new KAnimHashedString("wire2"),
            new KAnimHashedString("wire3"), new KAnimHashedString("wire4"),
        };

        private KBatchedAnimController controller;
        private LogicUtilityNetworkLink link;
        private bool tinted;

        protected override void OnSpawn()
        {
            base.OnSpawn();
            controller = GetComponent<KBatchedAnimController>();
            link = GetComponent<LogicUtilityNetworkLink>();
        }

        private void Update()
        {
            if (controller == null || link == null) return;

            bool overlayActive = OverlayScreen.Instance != null
                                 && OverlayScreen.Instance.mode == OverlayModes.Logic.ID;
            if (!overlayActive)
            {
                if (tinted)
                {
                    ResetTint();
                    tinted = false;
                }
                return;
            }

            var manager = Game.Instance != null ? Game.Instance.logicCircuitManager : null;
            if (manager == null) return;
            LogicCircuitNetwork network = manager.GetNetworkForCell(link.GetNetworkCell());

            Color32 on = GlobalAssets.Instance.colorSet.logicOn;
            Color32 off = GlobalAssets.Instance.colorSet.logicOff;
            on.a = 0;
            off.a = 0;

            if (!isRibbon)
            {
                controller.TintColour = network != null && network.IsBitActive(0) ? on : off;
            }
            else
            {
                for (int bit = 0; bit < WireSymbols.Length; bit++)
                {
                    controller.SetSymbolTint(WireSymbols[bit],
                        network != null && network.IsBitActive(bit) ? (Color)on : (Color)off);
                }
            }
            tinted = true;
        }

        private void ResetTint()
        {
            controller.TintColour = Color.white;
            if (isRibbon)
            {
                foreach (var sym in WireSymbols)
                {
                    controller.SetSymbolTint(sym, Color.white);
                }
            }
        }
    }
}
