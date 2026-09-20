using System;
using HarmonyLib;

namespace Dizzy.Nudge.Patches
{
    internal static class HammerNudgePatches
    {
        internal static void MainButtonDownPostfix(GoPointer __instance, ref bool __result)
        {
            if (!__result)
                return;

            try
            {
                GoPointerButton button = Traverse.Create(__instance).Field("pointedAtButton").GetValue<GoPointerButton>();
                if (NudgeMover.TryNudgeFromLeftClick(button, __instance))
                    __result = false;
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("Nudge left-click failed: " + e.Message);
            }
        }

        internal static bool OnAltActivatePrefix(ShipItemHammer __instance)
        {
            try
            {
                return !NudgeMover.TryNudgeFromRightClick(__instance);
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("Nudge right-click failed: " + e.Message);
                return true;
            }
        }

        internal static bool OnActivatePrefix(GoPointerButton __instance)
        {
            try
            {
                return !NudgeMover.ShouldInterceptLeftClick(__instance);
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("Nudge OnActivate failed: " + e.Message);
                return true;
            }
        }

        internal static bool OnActivatePointerPrefix(GoPointerButton __instance, GoPointer activatingPointer)
        {
            try
            {
                return !NudgeMover.TryNudgeFromLeftClick(__instance, activatingPointer);
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("Nudge OnActivate(pointer) failed: " + e.Message);
                return true;
            }
        }

        internal static bool OnItemClickPrefix(ShipItem __instance, PickupableItem heldItem, ref bool __result)
        {
            try
            {
                if (heldItem == null || heldItem.GetComponent<ShipItemHammer>() == null)
                    return true;
                if (NudgeMover.GetHeldAxis() == NudgeAxis.None)
                    return true;
                if (!NudgeMover.IsNudgeTarget(__instance))
                    return true;

                __result = false;
                return false;
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("Nudge OnItemClick failed: " + e.Message);
                return true;
            }
        }
    }
}
