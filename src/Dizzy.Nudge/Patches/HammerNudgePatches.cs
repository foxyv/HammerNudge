using HarmonyLib;

namespace Dizzy.Nudge.Patches
{
    internal static class HammerNudgePatches
    {
        internal static void MainButtonDownPostfix(GoPointer __instance, ref bool __result)
        {
            if (!__result)
                return;

            GoPointerButton button = Traverse.Create(__instance).Field("pointedAtButton").GetValue<GoPointerButton>();
            if (NudgeMover.TryNudgeFromLeftClick(button, __instance))
                __result = false;
        }

        internal static bool OnAltActivatePrefix(ShipItemHammer __instance)
        {
            return !NudgeMover.TryNudgeFromRightClick(__instance);
        }

        internal static bool OnActivatePrefix(GoPointerButton __instance)
        {
            return !NudgeMover.ShouldInterceptLeftClick(__instance);
        }

        internal static bool OnActivatePointerPrefix(GoPointerButton __instance, GoPointer activatingPointer)
        {
            return !NudgeMover.TryNudgeFromLeftClick(__instance, activatingPointer);
        }

        internal static bool OnItemClickPrefix(ShipItem __instance, PickupableItem heldItem, ref bool __result)
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
    }
}
