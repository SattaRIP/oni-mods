using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace MagpieExtension
{
    // Maps Magpie's gas/liquid bridge prefab tags to their conduit type.
    internal static class MagpieBridges
    {
        public static readonly Tag GasBridge2    = new Tag("qiguanqiao2");
        public static readonly Tag GasBridge3    = new Tag("qiguanqiao3");
        public static readonly Tag LiquidBridge2 = new Tag("shuiguanqiao2");
        public static readonly Tag LiquidBridge3 = new Tag("shuiguanqiao3");

        public static Type RegistrationType;
        public static MethodInfo RegisterMethod;
        public static FieldInfo Field_AllHandles;
        public static FieldInfo Field_HPA_GasBridge;
        public static FieldInfo Field_HPA_LiquidBridge;
        public static FieldInfo Field_All_GasBridge;
        public static FieldInfo Field_All_LiquidBridge;

        public static bool RonivansAvailable => RegistrationType != null;

        public static bool TryGetConduitType(Tag tag, out ConduitType type)
        {
            if (tag == GasBridge2 || tag == GasBridge3)       { type = ConduitType.Gas;    return true; }
            if (tag == LiquidBridge2 || tag == LiquidBridge3) { type = ConduitType.Liquid; return true; }
            type = default;
            return false;
        }

        public static ObjectLayer LayerFor(ConduitType type) => type == ConduitType.Gas
            ? ObjectLayer.GasConduitConnection
            : ObjectLayer.LiquidConduitConnection;

        // Direct cleanup of Ronivans' HP registry. Replicates Ronivans'
        // UnregisterHighPressureConduit body (HashSet<int>.Remove ops) without
        // going through their public method, to avoid Mono runtime issues that
        // surfaced when called via MethodInfo.Invoke during OnCleanUp.
        public static void DirectUnregister(GameObject go, ConduitType type)
        {
            if (go == null) return;
            var building = go.GetComponent<Building>();
            if (building == null) return;

            int instanceId = go.GetInstanceID();
            int inCell  = building.GetUtilityInputCell();
            int outCell = building.GetUtilityOutputCell();

            (Field_AllHandles?.GetValue(null) as HashSet<int>)?.Remove(instanceId);

            if (type == ConduitType.Gas)
            {
                var hpa = Field_HPA_GasBridge?.GetValue(null) as HashSet<int>;
                hpa?.Remove(inCell); hpa?.Remove(outCell);
                var all = Field_All_GasBridge?.GetValue(null) as HashSet<int>;
                all?.Remove(inCell); all?.Remove(outCell);
            }
            else
            {
                var hpa = Field_HPA_LiquidBridge?.GetValue(null) as HashSet<int>;
                hpa?.Remove(inCell); hpa?.Remove(outCell);
                var all = Field_All_LiquidBridge?.GetValue(null) as HashSet<int>;
                all?.Remove(inCell); all?.Remove(outCell);
            }
        }
    }

    // Marker component attached to Magpie's gas/liquid bridges in OnSpawn.
    // Its Unity OnDestroy runs the HP unregister when the GameObject is destroyed
    // (deconstruct, sandbox destroy, scene unload). This replaces the previous
    // Harmony patch on ConduitBridge.OnCleanUp, which was repeatedly crashing
    // through the dynamic-method wrapper with Mono runtime errors (first
    // "Illegal byte sequence", then "Method has zero rva"). Unity's component
    // lifecycle is reliable in a way Harmony's IL rewriting on this particular
    // method evidently is not.
    internal class MagpieHPMarker : MonoBehaviour
    {
        public ConduitType ConduitType;

        private void OnDestroy()
        {
            try
            {
                MagpieBridges.DirectUnregister(gameObject, ConduitType);
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[MagpieExtension] HP unregister failed on destroy: " + ex.Message);
            }
        }
    }

    // Register Magpie's gas/liquid bridges as high-pressure capable so Ronivans'
    // damage and flow-throttling logic treats them as HP bridges. Also attach
    // the MagpieHPMarker so cleanup happens via Unity's destroy lifecycle.
    [HarmonyPatch(typeof(ConduitBridge), "OnSpawn")]
    public static class ConduitBridge_OnSpawn_RegisterMagpieHP
    {
        [HarmonyPrepare]
        public static bool Prepare() => MagpieBridges.RonivansAvailable;

        public static void Postfix(ConduitBridge __instance)
        {
            try
            {
                var prefabID = __instance.GetComponent<KPrefabID>();
                if (prefabID == null) return;
                if (!MagpieBridges.TryGetConduitType(prefabID.PrefabTag, out var type)) return;

                MagpieBridges.RegisterMethod.Invoke(null, new object[]
                {
                    __instance.gameObject,
                    MagpieBridges.LayerFor(type),
                });

                var marker = __instance.gameObject.GetComponent<MagpieHPMarker>();
                if (marker == null) marker = __instance.gameObject.AddComponent<MagpieHPMarker>();
                marker.ConduitType = type;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("[MagpieExtension] OnSpawn HP register failed: " + ex.Message);
            }
        }
    }
}
