using System;
using System.Reflection;
using HarmonyLib;

namespace Dizzy.Nudge.Patches
{
    internal static class PatchApplier
    {
        internal static void Apply(Harmony harmony)
        {
            TryPrefix(
                harmony,
                typeof(ShipItemHammer),
                nameof(ShipItemHammer.OnAltActivate),
                Type.EmptyTypes,
                typeof(HammerNudgePatches),
                nameof(HammerNudgePatches.OnAltActivatePrefix),
                Priority.First);

            TryPrefix(
                harmony,
                typeof(ShipItem),
                nameof(ShipItem.OnItemClick),
                new[] { typeof(PickupableItem) },
                typeof(HammerNudgePatches),
                nameof(HammerNudgePatches.OnItemClickPrefix),
                Priority.First);

            // LampHook.OnItemClick overrides ShipItem and hangs HangableItems (the
            // hammer). Harmony does not run the base prefix on that override.
            TryPrefix(
                harmony,
                typeof(ShipItemLampHook),
                nameof(ShipItemLampHook.OnItemClick),
                new[] { typeof(PickupableItem) },
                typeof(HammerNudgePatches),
                nameof(HammerNudgePatches.OnItemClickPrefix),
                Priority.First);

            TryPostfix(
                harmony,
                typeof(LookUI),
                nameof(LookUI.ShowLookText),
                new[] { typeof(GoPointerButton) },
                typeof(LookUIPatches),
                nameof(LookUIPatches.ShowLookTextPostfix),
                Priority.First,
                after: new[] { "com.nandbrew.furniturefix" });

            TryPostfix(
                harmony,
                typeof(LookUI),
                "SetAltIcons",
                new[] { typeof(bool) },
                typeof(LookUIPatches),
                nameof(LookUIPatches.SetAltIconsPostfix),
                Priority.First);
        }

        private static void TryPrefix(
            Harmony harmony,
            Type target,
            string methodName,
            Type[] parameters,
            Type patchType,
            string patchMethod,
            int priority = Priority.Normal)
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
                var method = new HarmonyMethod(prefix) { priority = priority };
                harmony.Patch(original, prefix: method);
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
            string patchMethod,
            int priority = Priority.Normal,
            string[] after = null)
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
                var method = new HarmonyMethod(postfix) { priority = priority };
                if (after != null && after.Length > 0)
                    method.after = after;
                harmony.Patch(original, postfix: method);
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"Nudge: failed to patch {target.Name}.{methodName}: {e.Message}");
            }
        }
    }
}
