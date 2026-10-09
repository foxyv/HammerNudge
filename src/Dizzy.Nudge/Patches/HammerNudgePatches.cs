using System;
using HarmonyLib;

namespace Dizzy.Nudge.Patches
{
    internal static class HammerNudgePatches
    {
        private static bool _rawAltButtonDown;

        // Deft Hands reports right click as not pressed while its rotation key
        // (Left Alt by default) is held with an item in hand, so Alt+Q/T/E
        // right-click rotate never reached the hammer. Capture the game's answer
        // before that filter runs, then restore it afterwards for nudge rotates.
        internal static void AltButtonDownCapturePostfix(bool __result)
        {
            _rawAltButtonDown = __result;
        }

        internal static void AltButtonDownRestorePostfix(GoPointer __instance, ref bool __result)
        {
            if (__result || !_rawAltButtonDown || GameState.inCursorMenu)
                return;

            try
            {
                if (NudgeMover.IsRotateNudge(__instance))
                    __result = true;
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("Nudge right-click rotate failed: " + e.Message);
            }
        }

        internal static bool OnAltActivatePrefix(ShipItemHammer __instance)
        {
            try
            {
                if (NudgeMover.TryNudgeFromRightClick(__instance))
                    return false;
                if (NudgeMover.TryUnlockNailed(__instance))
                    return false;
                return true;
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("Nudge right-click failed: " + e.Message);
                return true;
            }
        }

        // Left-click nudge runs only while a hammer is already held. Empty-handed
        // pickup (beds, crates, bottles) never calls OnItemClick, so it stays vanilla.
        // Consume the click whenever Q/T/E is held so the hammer is not dropped or
        // hung, even if the look target is not a nudgeable item.
        internal static bool OnItemClickPrefix(ShipItem __instance, PickupableItem heldItem, ref bool __result)
        {
            try
            {
                if (!NudgeMover.IsNudgeClick(heldItem))
                    return true;

                NudgeMover.TryNudgeFromLeftClick(__instance, heldItem.held);
                __result = false;
                return false;
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("Nudge OnItemClick failed: " + e.Message);
                return true;
            }
        }

        // HooksHangMore hangs the held item from its own ShipItemLampHook.OnItemClick
        // prefix, and HarmonyX still runs it after ours returns false. A nudge click
        // then recorded the hammer as hanging on the hook, and the hook refused lamps
        // until the hammer was picked up again. Skip that prefix for nudge clicks.
        internal static bool HooksHangMoreLampHookClickPrefix(PickupableItem heldItem)
        {
            try
            {
                return !NudgeMover.IsNudgeClick(heldItem);
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("Nudge HooksHangMore skip failed: " + e.Message);
                return true;
            }
        }

        internal static bool DropItemPrefix(GoPointer __instance)
        {
            try
            {
                return !NudgeMover.ShouldKeepHammer(__instance);
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("Nudge DropItem skip failed: " + e.Message);
                return true;
            }
        }

        internal static bool OnDropPrefix(ShipItem __instance)
        {
            try
            {
                if (__instance == null || __instance.GetComponent<ShipItemHammer>() == null)
                    return true;
                return !NudgeMover.ShouldKeepHammer(__instance.held);
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("Nudge OnDrop skip failed: " + e.Message);
                return true;
            }
        }

        // Empty-look LMB throw charges currentThrowPower, then DropItem. Zero the
        // charge so a skipped drop cannot still fling the physics twin. Do not
        // reset timerAfterPickup: that froze the held hammer on the look ray
        // and left-click never hit the furniture.
        internal static void LateUpdatePrefix(GoPointer __instance)
        {
            try
            {
                if (!NudgeMover.ShouldKeepHammer(__instance))
                    return;

                Traverse.Create(__instance).Field("currentThrowPower").SetValue(0f);
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("Nudge keep-hammer failed: " + e.Message);
            }
        }
    }
}
