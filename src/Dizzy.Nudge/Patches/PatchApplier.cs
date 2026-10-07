using System;
using System.Reflection;
using BepInEx.Bootstrap;
using HarmonyLib;

namespace Dizzy.Nudge.Patches
{
    internal static class PatchApplier
    {
        internal const string HooksHangMoreGuid = "com.raddude.hookshangmore";
        private const string DeftHandsGuid = "com.keevi.defthands";
        private const string HooksHangMoreLampHookPatches = "HooksHangMore.AttachablePatches+ShipItemLampHookPatches";

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

            // Subclasses (bottles/barrels, lamp hooks, lights, …) override
            // OnItemClick; Harmony does not run the ShipItem prefix on those.
            PatchDeclaredOnItemClicks(harmony);
            PatchHooksHangMoreLampHook(harmony);

            TryPrefix(
                harmony,
                typeof(GoPointer),
                nameof(GoPointer.DropItem),
                Type.EmptyTypes,
                typeof(HammerNudgePatches),
                nameof(HammerNudgePatches.DropItemPrefix),
                Priority.First);

            TryPrefix(
                harmony,
                typeof(ShipItem),
                nameof(ShipItem.OnDrop),
                Type.EmptyTypes,
                typeof(HammerNudgePatches),
                nameof(HammerNudgePatches.OnDropPrefix),
                Priority.First);

            TryPrefix(
                harmony,
                typeof(GoPointer),
                "LateUpdate",
                Type.EmptyTypes,
                typeof(HammerNudgePatches),
                nameof(HammerNudgePatches.LateUpdatePrefix),
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

            // Deft Hands filters right click while Alt is held (see
            // HammerNudgePatches.AltButtonDownRestorePostfix). Bracket its postfix.
            TryPostfix(
                harmony,
                typeof(GoPointer),
                nameof(GoPointer.AltButtonDown),
                Type.EmptyTypes,
                typeof(HammerNudgePatches),
                nameof(HammerNudgePatches.AltButtonDownCapturePostfix),
                Priority.First,
                before: new[] { DeftHandsGuid });

            TryPostfix(
                harmony,
                typeof(GoPointer),
                nameof(GoPointer.AltButtonDown),
                Type.EmptyTypes,
                typeof(HammerNudgePatches),
                nameof(HammerNudgePatches.AltButtonDownRestorePostfix),
                Priority.Last,
                after: new[] { DeftHandsGuid });
        }

        private static void PatchDeclaredOnItemClicks(Harmony harmony)
        {
            Type[] types;
            try
            {
                types = AccessTools.GetTypesFromAssembly(typeof(ShipItem).Assembly);
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("Nudge: could not scan ShipItem click overrides: " + e.Message);
                TryPrefix(
                    harmony,
                    typeof(ShipItem),
                    nameof(ShipItem.OnItemClick),
                    new[] { typeof(PickupableItem) },
                    typeof(HammerNudgePatches),
                    nameof(HammerNudgePatches.OnItemClickPrefix),
                    Priority.First);
                return;
            }

            Type[] clickArgs = { typeof(PickupableItem) };
            for (int i = 0; i < types.Length; i++)
            {
                Type type = types[i];
                if (type == null || !typeof(ShipItem).IsAssignableFrom(type))
                    continue;
                if (AccessTools.DeclaredMethod(type, "OnItemClick", clickArgs) == null)
                    continue;

                TryPrefix(
                    harmony,
                    type,
                    "OnItemClick",
                    clickArgs,
                    typeof(HammerNudgePatches),
                    nameof(HammerNudgePatches.OnItemClickPrefix),
                    Priority.First);
            }
        }

        // Patches HooksHangMore's own lamp-hook click prefix (see
        // HammerNudgePatches.HooksHangMoreLampHookClickPrefix). The soft dependency
        // in Plugin loads HooksHangMore first, so its types resolve here.
        private static void PatchHooksHangMoreLampHook(Harmony harmony)
        {
            if (!Chainloader.PluginInfos.ContainsKey(HooksHangMoreGuid))
                return;

            Type patches = AccessTools.TypeByName(HooksHangMoreLampHookPatches);
            if (patches == null)
            {
                Plugin.Log.LogWarning("Nudge: HooksHangMore lamp-hook patch not found; a nudge click may still hang the hammer.");
                return;
            }

            TryPrefix(
                harmony,
                patches,
                "OnItemClick",
                null,
                typeof(HammerNudgePatches),
                nameof(HammerNudgePatches.HooksHangMoreLampHookClickPrefix),
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
            string[] after = null,
            string[] before = null)
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
                if (before != null && before.Length > 0)
                    method.before = before;
                harmony.Patch(original, postfix: method);
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"Nudge: failed to patch {target.Name}.{methodName}: {e.Message}");
            }
        }
    }
}
