using UnityEngine;

namespace MagpieExtension
{
    internal static class BridgeLink
    {
        // The vanilla WireBridgeConfig.AddNetworkLink hardcodes the bridge's
        // UtilityNetworkLink to CellOffset(-1,0) / CellOffset(+1,0) -- the endpoints
        // of the native 1-tile (3-wide) bridge. Our wide variants delegate
        // DoPostConfigureComplete to that base config, so every wide power/conductive/
        // insulated wire bridge inherits the +/-1 link. That pins BOTH the electrical
        // connection AND the displayed sockets at +/-1 regardless of building width:
        // the bridge only actually spans a 1-tile gap, and the sockets render inboard
        // of the terminal art. Re-point the link(s) to the building's real input/output
        // offsets after delegating to the base so the bridge spans its full gap and the
        // sockets sit on the terminals. link1/link2 are public CellOffset fields on
        // UtilityNetworkLink (WireUtilityNetworkLink / LogicUtilityNetworkLink derive
        // from it), set on the prefab before spawn so placed + loaded instances update.
        public static void Repoint(GameObject go, CellOffset input, CellOffset output)
        {
            foreach (var link in go.GetComponents<UtilityNetworkLink>())
            {
                link.link1 = input;
                link.link2 = output;
            }
        }
    }
}
