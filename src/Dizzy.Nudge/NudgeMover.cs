using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace Dizzy.Nudge
{
    internal enum NudgeAxis
    {
        None,
        Away,
        Vertical,
        Strafe
    }

    internal static class NudgeMover
    {
        internal static NudgeAxis GetHeldAxis()
        {
            if (NudgeConfig.Enabled == null || !NudgeConfig.Enabled.Value)
                return NudgeAxis.None;

            if (IsPressed(NudgeConfig.AwayKey.Value))
                return NudgeAxis.Away;
            if (IsPressed(NudgeConfig.VerticalKey.Value))
                return NudgeAxis.Vertical;
            if (IsPressed(NudgeConfig.StrafeKey.Value))
                return NudgeAxis.Strafe;

            return NudgeAxis.None;
        }

        internal static bool IsNudgeTarget(ShipItem item)
        {
            if (item == null || !item.sold || !item.nailed)
                return false;

            return ShipItemHammer.CanNail(item);
        }

        internal static bool HoldingHammer(GoPointer pointer)
        {
            if (pointer == null)
                return false;

            PickupableItem held = pointer.GetHeldItem();
            return held != null && held.GetComponent<ShipItemHammer>() != null;
        }

        internal static GoPointer GetLookingPointer(GoPointerButton button)
        {
            if (button == null)
                return null;

            return Traverse.Create(button).Field("pointedAtBy").GetValue<GoPointer>();
        }

        internal static bool ShouldInterceptLeftClick(GoPointerButton button)
        {
            if (GetHeldAxis() == NudgeAxis.None)
                return false;

            ShipItem item = button != null ? button.GetComponent<ShipItem>() : null;
            if (!IsNudgeTarget(item))
                return false;

            GoPointer pointer = GetLookingPointer(button);
            return HoldingHammer(pointer);
        }

        internal static bool TryNudgeFromLeftClick(GoPointerButton button, GoPointer pointer)
        {
            if (!HoldingHammer(pointer))
                return false;

            ShipItem item = button != null ? button.GetComponent<ShipItem>() : null;
            return TryNudge(item, pointer, rightClick: false);
        }

        internal static bool TryNudgeFromRightClick(ShipItemHammer hammer)
        {
            if (hammer == null || hammer.held == null)
                return false;

            return TryNudge(hammer.held.GetPointedAtItem(), hammer.held, rightClick: true);
        }

        internal static bool TryNudge(ShipItem item, GoPointer pointer, bool rightClick)
        {
            NudgeAxis axis = GetHeldAxis();
            if (axis == NudgeAxis.None || pointer == null || !IsNudgeTarget(item))
                return false;

            Transform look = LookTransform(pointer);
            Vector3 delta = ComputeDelta(item, look, axis, rightClick);
            if (delta.sqrMagnitude < 1e-12f)
                return false;

            if (!Apply(item, delta))
                return false;

            PlayTap();
            return true;
        }

        internal static string GetPrompt(NudgeAxis axis)
        {
            switch (axis)
            {
                case NudgeAxis.Away:
                    return "nudge away\nnudge closer";
                case NudgeAxis.Vertical:
                    return "nudge up\nnudge down";
                case NudgeAxis.Strafe:
                    return "nudge left\nnudge right";
                default:
                    return null;
            }
        }

        private static Transform LookTransform(GoPointer pointer)
        {
            if (Camera.main != null)
                return Camera.main.transform;
            return pointer != null ? pointer.transform : null;
        }

        private static Vector3 ComputeDelta(ShipItem item, Transform look, NudgeAxis axis, bool rightClick)
        {
            if (look == null)
                return Vector3.zero;

            Vector3 up = DeckUp(item);
            Vector3 away = Vector3.ProjectOnPlane(look.forward, up);
            if (away.sqrMagnitude < 0.0001f)
                away = Vector3.Cross(up, look.right);
            away.Normalize();

            Vector3 right = Vector3.Cross(up, away);
            if (right.sqrMagnitude < 0.0001f)
                right = Vector3.ProjectOnPlane(look.right, up);
            right.Normalize();

            up = Vector3.Cross(away, right);
            up.Normalize();

            Vector3 dir;
            switch (axis)
            {
                case NudgeAxis.Away:
                    dir = rightClick ? -away : away;
                    break;
                case NudgeAxis.Vertical:
                    dir = rightClick ? -up : up;
                    break;
                case NudgeAxis.Strafe:
                    dir = rightClick ? right : -right;
                    break;
                default:
                    return Vector3.zero;
            }

            return dir * GetStep();
        }

        private static Vector3 DeckUp(ShipItem item)
        {
            // Walk colliders are often rotated/scaled boxes; their .up is not the deck.
            if (item.currentActualBoat != null)
                return item.currentActualBoat.up;
            return Vector3.up;
        }

        private static float GetStep()
        {
            if (IsFinePressed())
                return NudgeConfig.FineStepMeters.Value;
            return NudgeConfig.StepMeters.Value;
        }

        private static bool Apply(ShipItem item, Vector3 delta)
        {
            ItemRigidbody twin = item.GetItemRigidbody();
            if (twin == null)
            {
                Plugin.Log.LogWarning("Nudge: missing ItemRigidbody on " + item.name + ", skip.");
                return false;
            }

            // Visual is parented to the boat mesh; the physics twin is parented to the
            // walk collider. Those transforms often do not share world axes, so a world
            // delta on the twin is remapped diagonally onto the mesh. Move the visual,
            // then copy into the twin with the same local-space mapping the game uses.
            item.transform.position += delta;

            Transform boat = item.currentActualBoat;
            Transform walk = item.currentWalkCol;
            if (boat != null && walk != null)
            {
                Vector3 boatLocal = boat.InverseTransformPoint(item.transform.position);
                Quaternion boatLocalRot = Quaternion.Inverse(boat.rotation) * item.transform.rotation;
                twin.transform.position = walk.TransformPoint(boatLocal);
                twin.transform.rotation = walk.rotation * boatLocalRot;
            }
            else
            {
                twin.transform.position += delta;
            }

            return true;
        }

        private static void PlayTap()
        {
            if (UISoundPlayer.instance == null)
                return;

            UISoundPlayer.instance.PlayUISound(UISounds.winchClick, 0.35f, 1.15f);
        }

        private static bool IsPressed(KeyboardShortcut shortcut)
        {
            if (shortcut.MainKey == KeyCode.None)
                return false;

            // Do not use KeyboardShortcut.IsPressed(): BepInEx requires an exact
            // modifier set, so Shift+Q fails when AwayKey is just Q.
            if (!KeyIsDown(shortcut.MainKey))
                return false;

            foreach (var modifier in shortcut.Modifiers)
            {
                if (!KeyIsDown(modifier))
                    return false;
            }

            return true;
        }

        private static bool IsFinePressed()
        {
            return KeyIsDown(NudgeConfig.FineModifier.Value.MainKey);
        }

        private static bool KeyIsDown(KeyCode key)
        {
            if (key == KeyCode.None)
                return false;
            if (key == KeyCode.LeftShift || key == KeyCode.RightShift)
                return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            if (key == KeyCode.LeftControl || key == KeyCode.RightControl)
                return Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
            if (key == KeyCode.LeftAlt || key == KeyCode.RightAlt)
                return Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);

            return Input.GetKey(key);
        }
    }
}
