using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

namespace MagpieExtension
{
    // Diagnostic + guard patch for the intermittent save crash:
    //
    //   ExecutionEngineException: String conversion error: Illegal byte sequence ...
    //     at System.RuntimeType.getFullName(...)
    //     at System.RuntimeType.ToString()
    //     at SaveLoadRoot.SaveWithoutTransform (BinaryWriter writer) [0x000dd]
    //
    // The crash site is `component.GetType().ToString()` -- ONI writes the runtime
    // type name into the save for each KMonoBehaviour. Some component on some
    // SaveLoadRoot has a Type whose Mono metadata is unreadable (corruption, or
    // a dynamically-generated type with bad name bytes). The exception bubbles
    // up through SaveManager.Save and aborts the whole save.
    //
    // We transpile SaveLoadRoot.SaveWithoutTransform to route that one ToString
    // call through SafeTypeToString below, which:
    //   1. Tries the normal ToString().
    //   2. On failure, logs a WARNING naming the GameObject and the best fallback
    //      identifier we can extract from the broken Type (Name, then a constant).
    //   3. Returns a sentinel string so the save continues. The slot will be
    //      written with the sentinel as its type name -- on reload that component
    //      won't be reconstructed, but the rest of the save is intact.
    //
    // Net effect:
    //   * Save no longer aborts because of one bad component.
    //   * The Player.log gains a "[MagpieExtension/SaveCrashGuard] ..." line
    //     identifying the offender every time it triggers, which is the
    //     debugging info we've been missing.
    [HarmonyPatch(typeof(SaveLoadRoot), nameof(SaveLoadRoot.SaveWithoutTransform))]
    public static class SaveLoadRoot_SaveWithoutTransform_Guard
    {
        // Tracks the GameObject currently being saved so the helper can name
        // it in the warning even though the Transpiler-injected call only has
        // the Type on the stack. Updated by the Prefix below per-instance.
        [ThreadStatic] private static string _currentGameObjectName;

        public static void Prefix(SaveLoadRoot __instance)
        {
            try
            {
                _currentGameObjectName = __instance != null && __instance.gameObject != null
                    ? __instance.gameObject.name
                    : "<null>";
            }
            catch
            {
                _currentGameObjectName = "<unreadable>";
            }
        }

        public static void Postfix()
        {
            _currentGameObjectName = null;
        }

        // Replaces the single `callvirt instance string object::ToString()` that
        // immediately follows `callvirt Type object::GetType()` with a call to
        // SafeTypeToString. The adjacency requirement keeps the match precise;
        // we deliberately do NOT compare DeclaringType, because on U59 the raw
        // IL shows Object::GetType/Object::ToString yet a DeclaringType ==
        // typeof(object) check stopped matching -- Harmony's operand resolution
        // evidently presents the members differently than the metadata ref.
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var src = new List<CodeInstruction>(instructions);
            var safeTypeToString = AccessTools.Method(
                typeof(SaveLoadRoot_SaveWithoutTransform_Guard),
                nameof(SafeTypeToString));

            bool matched = false;
            for (int i = 0; i < src.Count - 1; i++)
            {
                if (!IsNoArgCall(src[i], "GetType")) continue;
                if (!IsNoArgCall(src[i + 1], "ToString")) continue;
                // Replace the ToString callvirt with a static call to
                // SafeTypeToString(Type) -> string. Stack effect matches:
                // pops Type, pushes string.
                src[i + 1] = new CodeInstruction(OpCodes.Call, safeTypeToString);
                matched = true;
                break;
            }

            if (!matched)
            {
                // Dump the call sequence Harmony actually sees, so the log tells
                // us what to match against next time instead of leaving us blind.
                var calls = new List<string>();
                foreach (var insn in src)
                {
                    if (insn.operand is MethodInfo m &&
                        (insn.opcode == OpCodes.Call || insn.opcode == OpCodes.Callvirt))
                        calls.Add((m.DeclaringType != null ? m.DeclaringType.Name : "?") + "." + m.Name);
                }
                Debug.LogWarning("[MagpieExtension/SaveCrashGuard] Transpiler did NOT find " +
                    "the GetType+ToString pattern in SaveLoadRoot.SaveWithoutTransform. " +
                    "Save-crash guard is INACTIVE. Calls seen (" + calls.Count + "): " +
                    string.Join(" | ", calls.ToArray()));
            }

            return src;
        }

        private static bool IsNoArgCall(CodeInstruction insn, string methodName)
        {
            if (insn.opcode != OpCodes.Callvirt && insn.opcode != OpCodes.Call) return false;
            var mi = insn.operand as MethodInfo;
            if (mi == null) return false;
            if (mi.Name != methodName) return false;
            if (mi.GetParameters().Length != 0) return false;
            return true;
        }

        // The actual guard. Called for every component being saved.
        //
        // Hot path: just `return type.ToString()`. The cost is one extra
        // managed call per saved component, dwarfed by the BinaryWriter cost.
        //
        // Cold path (the crash we're guarding): log everything we can extract
        // and return a sentinel so the save continues.
        public static string SafeTypeToString(Type type)
        {
            if (type == null)
            {
                Debug.LogWarning("[MagpieExtension/SaveCrashGuard] component.GetType() returned null on '"
                    + (_currentGameObjectName ?? "?") + "'. Writing sentinel.");
                return "__MagpieSaveGuard_NullType__";
            }

            try
            {
                return type.ToString();
            }
            catch (Exception ex)
            {
                string fallback = TryGetTypeName(type);
                Debug.LogWarning("[MagpieExtension/SaveCrashGuard] Type.ToString() threw "
                    + ex.GetType().Name + " on '" + (_currentGameObjectName ?? "?")
                    + "'. Best-effort type identifier: '" + fallback
                    + "'. Save will continue with a sentinel for this slot; "
                    + "the component will be missing after reload. Cause: " + ex.Message);
                return "__MagpieSaveGuard_BadType_" + fallback + "__";
            }
        }

        // Type.FullName / Type.Name go through the same native getFullName path
        // for most cases, so they can throw the same exception. We try each in
        // turn, swallowing failures, and finally fall back to a constant.
        private static string TryGetTypeName(Type type)
        {
            try { var n = type.FullName; if (!string.IsNullOrEmpty(n)) return n; } catch { }
            try { var n = type.Name;     if (!string.IsNullOrEmpty(n)) return n; } catch { }
            try { return "hash:" + type.GetHashCode().ToString("X"); } catch { }
            return "unreadable";
        }
    }
}
