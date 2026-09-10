using System;
using System.Reflection;
using HarmonyLib;

namespace Dizzy.Nudge.Patches
{
    internal static class PatchApplier
    {
        internal static void Apply(Harmony harmony)
        {
            TryPostfix(
                harmony,
                typeof(GoPointer),
                nameof(GoPointer.MainButtonDown),
                Type.EmptyTypes,
                typeof(HammerNudgePatches),
                nameof(HammerNudgePatches.MainButtonDownPostfix));

            TryPrefix(
                harmony,
                typeof(ShipItemHammer),
                nameof(ShipItemHammer.OnAltActivate),
                Type.EmptyTypes,
                typeof(HammerNudgePatches),
                nameof(HammerNudgePatches.OnAltActivatePrefix));

            TryPrefix(
                harmony,
                typeof(GoPointerButton),
                nameof(GoPointerButton.OnActivate),
                Type.EmptyTypes,
                typeof(HammerNudgePatches),
                nameof(HammerNudgePatches.OnActivatePrefix));

            TryPrefix(
                harmony,
                typeof(GoPointerButton),
                nameof(GoPointerButton.OnActivate),
                new[] { typeof(GoPointer) },
                typeof(HammerNudgePatches),
                nameof(HammerNudgePatches.OnActivatePointerPrefix));

            TryPrefix(
                harmony,
                typeof(ShipItem),
                nameof(ShipItem.OnItemClick),
                new[] { typeof(PickupableItem) },
                typeof(HammerNudgePatches),
                nameof(HammerNudgePatches.OnItemClickPrefix));

            TryPostfix(
                harmony,
                typeof(LookUI),
                nameof(LookUI.ShowLookText),
                new[] { typeof(GoPointerButton) },
                typeof(LookUIPatches),
                nameof(LookUIPatches.ShowLookTextPostfix));
        }

        private static void TryPrefix(
            Harmony harmony,
            Type target,
            string methodName,
            Type[] parameters,
            Type patchType,
            string patchMethod)
        {
            MethodInfo original = AccessTools.DeclaredMethod(target, methodName, parameters);
            MethodInfo prefix = AccessTools.DeclaredMethod(patchType, patchMethod);
            if (original == null || prefix == null)
            {
                Plugin.Log.LogWarning($"Nudge: missing {target.Name}.{methodName}, skip patch.");
                return;
            }

            try
            {
                harmony.Patch(original, prefix: new HarmonyMethod(prefix));
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"Nudge: failed to patch {target.Name}.{methodName}: {e.Message}");
            }
        }

        private static void TryPostfix(
            Harmony harmony,
            Type target,
            string methodName,
            Type[] parameters,
            Type patchType,
            string patchMethod)
        {
            MethodInfo original = AccessTools.DeclaredMethod(target, methodName, parameters);
            MethodInfo postfix = AccessTools.DeclaredMethod(patchType, patchMethod);
            if (original == null || postfix == null)
            {
                Plugin.Log.LogWarning($"Nudge: missing {target.Name}.{methodName}, skip patch.");
                return;
            }

            try
            {
                harmony.Patch(original, postfix: new HarmonyMethod(postfix));
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"Nudge: failed to patch {target.Name}.{methodName}: {e.Message}");
            }
        }
    }
}
