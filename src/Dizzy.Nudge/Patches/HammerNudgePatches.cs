using System;
using HarmonyLib;

namespace Dizzy.Nudge.Patches
{
    internal static class HammerNudgePatches
    {
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

        // Left-click nudge runs only while a hammer is already held. Empty-handed
        // pickup (beds, crates, bottles) never calls OnItemClick, so it stays vanilla.
        internal static bool OnItemClickPrefix(ShipItem __instance, PickupableItem heldItem, ref bool __result)
        {
            try
            {
                if (heldItem == null || heldItem.GetComponent<ShipItemHammer>() == null)
                    return true;

                if (!NudgeMover.TryNudgeFromLeftClick(__instance, heldItem.held))
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
