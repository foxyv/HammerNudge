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

        internal static bool IsRotateHeld()
        {
            if (NudgeConfig.RotateModifier == null)
                return false;
            return IsModifierHeld(NudgeConfig.RotateModifier.Value);
        }

        internal static bool IsNudgeTarget(ShipItem item)
        {
            // Do not call ShipItemHammer.CanNail: other hammer mods postfix it, and
            // an exception there would abort our click postfix and break the hammer.
            // Locked (nailed) beds can be nudged; unlocked beds stay vanilla pickup.
            return item != null && item.sold && item.nailed;
        }

        // A left click with the hammer while Q/T/E is held is a nudge: never a
        // hang, drop, or any other item-on-item click.
        internal static bool IsNudgeClick(PickupableItem heldItem)
        {
            return heldItem != null
                && heldItem.GetComponent<ShipItemHammer>() != null
                && GetHeldAxis() != NudgeAxis.None;
        }

        // True when this pointer's right click would rotate a locked item: hammer
        // in hand, RotateModifier and Q/T/E held, looking at a nudge target.
        internal static bool IsRotateNudge(GoPointer pointer)
        {
            return GetHeldAxis() != NudgeAxis.None
                && IsRotateHeld()
                && HoldingHammer(pointer)
                && IsNudgeTarget(pointer.GetPointedAtItem());
        }

        internal static bool ShouldKeepHammer(GoPointer pointer)
        {
            if (NudgeConfig.Enabled == null || !NudgeConfig.Enabled.Value)
                return false;
            if (GetHeldAxis() == NudgeAxis.None)
                return false;
            return HoldingHammer(pointer);
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

        internal static bool TryNudgeFromLeftClick(GoPointerButton button, GoPointer pointer)
        {
            // Empty hands must always fall through to vanilla pickup (beds, crates, etc.).
            if (!HoldingHammer(pointer))
                return false;

            ShipItem item = button != null ? button.GetComponent<ShipItem>() : null;
            if (item == null || !item.nailed)
                return false;

            return TryNudge(item, pointer, rightClick: false);
        }

        internal static bool TryNudgeFromRightClick(ShipItemHammer hammer)
        {
            if (hammer == null || hammer.held == null)
                return false;

            return TryNudge(hammer.held.GetPointedAtItem(), hammer.held, rightClick: true);
        }

        // Empty barrels can sit at big=false, so vanilla CanNail is false and
        // OnAltActivate never un-nails. Do not call CanNail (other hammer mods
        // postfix it). Unlock any sold nailed look-target when Q/T/E is up.
        internal static bool TryUnlockNailed(ShipItemHammer hammer)
        {
            if (NudgeConfig.Enabled == null || !NudgeConfig.Enabled.Value)
                return false;
            if (hammer == null || hammer.held == null)
                return false;
            if (GetHeldAxis() != NudgeAxis.None)
                return false;

            ShipItem item = hammer.held.GetPointedAtItem();
            if (!IsNudgeTarget(item))
                return false;

            item.nailed = false;
            if (UISoundPlayer.instance != null)
                UISoundPlayer.instance.PlayUISound(UISounds.winchUnclick, 1f, 0.7f);
            return true;
        }

        internal static bool TryNudge(ShipItem item, GoPointer pointer, bool rightClick)
        {
            NudgeAxis axis = GetHeldAxis();
            if (axis == NudgeAxis.None || pointer == null || !IsNudgeTarget(item))
                return false;

            Transform look = LookTransform(pointer);
            if (look == null)
                return false;

            Vector3 up;
            Vector3 away;
            Vector3 right;
            if (!TryDeckAxes(item, look, out up, out away, out right))
                return false;

            if (IsRotateHeld())
            {
                if (!TryRotate(item, axis, rightClick, up, away, right))
                    return false;
            }
            else
            {
                Vector3 delta = ComputeDelta(axis, rightClick, up, away, right);
                if (delta.sqrMagnitude < 1e-12f)
                    return false;
                item.transform.position += delta;
                if (!SyncTwin(item))
                    return false;
            }

            PlayTap();
            return true;
        }

        internal static bool WasLevelPressed()
        {
            if (NudgeConfig.Enabled == null || !NudgeConfig.Enabled.Value)
                return false;
            if (NudgeConfig.LevelKey == null)
                return false;
            return KeyWentDown(NudgeConfig.LevelKey.Value.MainKey);
        }

        internal static bool TryLevelFromLook()
        {
            GoPointer[] pointers = Object.FindObjectsOfType<GoPointer>();
            for (int i = 0; i < pointers.Length; i++)
            {
                GoPointer pointer = pointers[i];
                if (!HoldingHammer(pointer))
                    continue;

                ShipItem item = pointer.GetPointedAtItem();
                if (!IsNudgeTarget(item))
                    continue;

                if (TryLevel(item))
                    return true;
            }

            return false;
        }

        internal static string GetPrompt(NudgeAxis axis)
        {
            if (IsRotateHeld())
            {
                switch (axis)
                {
                    case NudgeAxis.Away:
                        return "tilt away\ntilt closer";
                    case NudgeAxis.Vertical:
                        return "roll left\nroll right";
                    case NudgeAxis.Strafe:
                        return "turn left\nturn right";
                    default:
                        return KeyLabel(NudgeConfig.RotateModifier) + "+"
                            + KeyLabel(NudgeConfig.AwayKey) + "/"
                            + KeyLabel(NudgeConfig.VerticalKey) + "/"
                            + KeyLabel(NudgeConfig.StrafeKey) + " rotate\nunlock";
                }
            }

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

        internal static string GetIdleControls()
        {
            return KeyLabel(NudgeConfig.AwayKey) + "/"
                + KeyLabel(NudgeConfig.VerticalKey) + "/"
                + KeyLabel(NudgeConfig.StrafeKey) + " nudge  "
                + KeyLabel(NudgeConfig.LevelKey) + " level\nunlock";
        }

        private static string KeyLabel(ConfigEntry<KeyboardShortcut> entry)
        {
            if (entry == null || entry.Value.MainKey == KeyCode.None)
                return "?";
            KeyCode key = entry.Value.MainKey;
            if (key == KeyCode.LeftAlt || key == KeyCode.RightAlt)
                return "Alt";
            if (key == KeyCode.LeftShift || key == KeyCode.RightShift)
                return "Shift";
            if (key == KeyCode.LeftControl || key == KeyCode.RightControl)
                return "Ctrl";
            return key.ToString();
        }

        private static Transform LookTransform(GoPointer pointer)
        {
            if (Camera.main != null)
                return Camera.main.transform;
            return pointer != null ? pointer.transform : null;
        }

        private static bool TryDeckAxes(
            ShipItem item,
            Transform look,
            out Vector3 up,
            out Vector3 away,
            out Vector3 right)
        {
            up = DeckUp(item);
            away = Vector3.ProjectOnPlane(look.forward, up);
            if (away.sqrMagnitude < 0.0001f)
                away = Vector3.Cross(up, look.right);
            if (away.sqrMagnitude < 0.0001f)
            {
                right = Vector3.zero;
                return false;
            }

            away.Normalize();
            right = Vector3.Cross(up, away);
            if (right.sqrMagnitude < 0.0001f)
                right = Vector3.ProjectOnPlane(look.right, up);
            if (right.sqrMagnitude < 0.0001f)
                return false;

            right.Normalize();
            up = Vector3.Cross(away, right);
            up.Normalize();
            return true;
        }

        private static Vector3 ComputeDelta(
            NudgeAxis axis,
            bool rightClick,
            Vector3 up,
            Vector3 away,
            Vector3 right)
        {
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

            return dir * GetMoveStep();
        }

        private static bool TryRotate(
            ShipItem item,
            NudgeAxis axis,
            bool rightClick,
            Vector3 up,
            Vector3 away,
            Vector3 right)
        {
            Vector3 pivot;
            switch (axis)
            {
                case NudgeAxis.Away:
                    pivot = right;
                    break;
                case NudgeAxis.Vertical:
                    pivot = -away;
                    break;
                case NudgeAxis.Strafe:
                    pivot = up;
                    break;
                default:
                    return false;
            }

            float angle = GetRotateStep();
            if (rightClick)
                angle = -angle;

            item.transform.rotation = Quaternion.AngleAxis(angle, pivot) * item.transform.rotation;
            return SyncTwin(item);
        }

        private static bool TryLevel(ShipItem item)
        {
            Vector3 up = DeckUp(item);
            Vector3 from = ClosestLocalAxis(item.transform, up);
            Quaternion flatten = Quaternion.FromToRotation(from, up);
            if (flatten == Quaternion.identity)
            {
                PlayTap();
                return true;
            }

            // Rotate in place, then keep the collider's underside at the same
            // deck height so a barrel does not hop when its mesh origin is low.
            float before = ContactHeight(item, up);
            item.transform.rotation = flatten * item.transform.rotation;
            float after = ContactHeight(item, up);
            item.transform.position += up * (before - after);

            if (!SyncTwin(item))
                return false;

            PlayTap();
            return true;
        }

        // Furniture meshes do not all use local Y as visual up. Align whichever
        // local axis is already closest to deck-up so Level does not yaw a rack
        // onto its side.
        private static Vector3 ClosestLocalAxis(Transform t, Vector3 deckUp)
        {
            Vector3[] axes =
            {
                t.up, -t.up, t.forward, -t.forward, t.right, -t.right
            };
            Vector3 best = t.up;
            float bestDot = float.NegativeInfinity;
            for (int i = 0; i < axes.Length; i++)
            {
                float d = Vector3.Dot(axes[i], deckUp);
                if (d > bestDot)
                {
                    bestDot = d;
                    best = axes[i];
                }
            }

            return best;
        }

        private static float ContactHeight(ShipItem item, Vector3 up)
        {
            Collider col = item.GetComponent<Collider>();
            Vector3 point = item.transform.position;
            if (col != null)
                point = col.ClosestPoint(item.transform.position - up * 20f);
            return Vector3.Dot(point, up);
        }

        private static Vector3 DeckUp(ShipItem item)
        {
            // Walk colliders are often rotated/scaled boxes; their .up is not the deck.
            if (item.currentActualBoat != null)
                return item.currentActualBoat.up;
            return Vector3.up;
        }

        private static float GetMoveStep()
        {
            if (IsFinePressed())
                return NudgeConfig.FineStepMeters.Value;
            return NudgeConfig.StepMeters.Value;
        }

        private static float GetRotateStep()
        {
            if (IsFinePressed())
                return NudgeConfig.FineStepDegrees.Value;
            return NudgeConfig.StepDegrees.Value;
        }

        private static bool SyncTwin(ShipItem item)
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
                twin.transform.position = item.transform.position;
                twin.transform.rotation = item.transform.rotation;
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

        private static bool IsModifierHeld(KeyboardShortcut shortcut)
        {
            if (shortcut.MainKey == KeyCode.None)
                return false;
            return KeyIsDown(shortcut.MainKey);
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

        private static bool KeyWentDown(KeyCode key)
        {
            if (key == KeyCode.None)
                return false;
            if (key == KeyCode.LeftShift || key == KeyCode.RightShift)
                return Input.GetKeyDown(KeyCode.LeftShift) || Input.GetKeyDown(KeyCode.RightShift);
            if (key == KeyCode.LeftControl || key == KeyCode.RightControl)
                return Input.GetKeyDown(KeyCode.LeftControl) || Input.GetKeyDown(KeyCode.RightControl);
            if (key == KeyCode.LeftAlt || key == KeyCode.RightAlt)
                return Input.GetKeyDown(KeyCode.LeftAlt) || Input.GetKeyDown(KeyCode.RightAlt);

            return Input.GetKeyDown(key);
        }
    }
}
