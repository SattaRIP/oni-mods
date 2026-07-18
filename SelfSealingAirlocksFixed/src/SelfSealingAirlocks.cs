using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

// Port of chromiumboy's Self-Sealing Airlocks (via "Self-sealing Airlocks Revived")
// to Harmony 2 + the U59-740622 Sim API (SetCellProperties/ClearCellProperties
// gained an optional callbackIdx parameter, which broke the old binary).
// Original source: github.com/chromiumboy/OxygenNotIncludedMods (MIT-style, author
// welcomes maintainers).

namespace SelfSealingAirlocks
{
    public sealed class SelfSealingAirlocksMod : KMod.UserMod2
    {
    }

    // Add anim override (necessary to prevent game crash)
    [HarmonyPatch(typeof(Door), "OnPrefabInit")]
    internal class SelfSealingAirlocks_Door_OnPrefabInit
    {
        private static void Postfix(ref Door __instance)
        {
            __instance.overrideAnims = new KAnimFile[]
            {
                Assets.GetAnim("anim_use_remote_kanim")
            };
        }
    }

    // Ensure cell properties are cleared on clean up
    [HarmonyPatch(typeof(Door), "OnCleanUp")]
    internal class SelfSealingAirlocks_Door_OnCleanUp
    {
        private static void Postfix(Door __instance)
        {
            foreach (int cell in __instance.building.PlacementCells)
            {
                SimMessages.ClearCellProperties(cell, 3);
            }
        }
    }

    // Update sim state setter to make airlock doors gas impermeable
    [HarmonyPatch(typeof(Door), "SetSimState")]
    internal class SelfSealingAirlocks_Door_SetSimState
    {
        private static bool Prefix(Door __instance, bool is_door_open, IList<int> cells)
        {
            if (__instance.gameObject == null)
            { return true; }

            Door.ControlState controlState = Traverse.Create(__instance).Field("controlState").GetValue<Door.ControlState>();
            Door.DoorType doorType = __instance.doorType;

            // Internal doors and doors pinned open keep vanilla behavior
            if (doorType == Door.DoorType.Internal || controlState == Door.ControlState.Opened)
            { return true; }

            PrimaryElement element = __instance.GetComponent<PrimaryElement>();
            float mass_per_cell = element.Mass / cells.Count;

            for (int i = 0; i < cells.Count; i++)
            {
                int cell = cells[i];
                SimMessages.SetCellProperties(cell, 4);

                if (is_door_open)
                {
                    MethodInfo method_opened = AccessTools.Method(typeof(Door), "OnSimDoorOpened", null, null);
                    System.Action cb_opened = (System.Action)Delegate.CreateDelegate(typeof(System.Action), __instance, method_opened);
                    HandleVector<Game.CallbackInfo>.Handle handle = Game.Instance.callbackManager.Add(new Game.CallbackInfo(cb_opened, false));
                    SimMessages.ReplaceAndDisplaceElement(cell, element.ElementID, CellEventLogger.Instance.DoorOpen, mass_per_cell, element.Temperature, byte.MaxValue, 0, handle.index);
                }
                else
                {
                    MethodInfo method_closed = AccessTools.Method(typeof(Door), "OnSimDoorClosed", null, null);
                    System.Action cb_closed = (System.Action)Delegate.CreateDelegate(typeof(System.Action), __instance, method_closed);
                    HandleVector<Game.CallbackInfo>.Handle handle = Game.Instance.callbackManager.Add(new Game.CallbackInfo(cb_closed, false));
                    SimMessages.ReplaceAndDisplaceElement(cell, element.ElementID, CellEventLogger.Instance.DoorClose, mass_per_cell, element.Temperature, byte.MaxValue, 0, handle.index);
                }
            }

            // Skip the original method
            return false;
        }
    }
}
