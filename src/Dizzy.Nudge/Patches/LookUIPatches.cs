using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace Dizzy.Nudge.Patches
{
    internal static class LookUIPatches
    {
        private static MethodInfo _getKeyCode;
        private static object _pickUp;
        private static object _activate;
        private static bool _resolvedKeys;

        internal static void ShowLookTextPostfix(LookUI __instance, GoPointerButton button)
        {
            try
            {
                if (__instance == null || button == null)
                    return;
                if (NudgeConfig.Enabled == null || !NudgeConfig.Enabled.Value)
                    return;

                Traverse look = Traverse.Create(__instance);
                GoPointer pointer = look.Field("pointer").GetValue<GoPointer>();
                if (pointer == null)
                    pointer = NudgeMover.GetLookingPointer(button);

                bool hammer = NudgeMover.HoldingHammer(pointer);
                bool bed = IsBed(button);
                ShipItem item = button.GetComponent<ShipItem>();

                if (hammer && NudgeMover.IsNudgeTarget(item))
                {
                    TextMesh controls = look.Field("controlsText").GetValue<TextMesh>();
                    if (controls != null)
                    {
                        NudgeAxis axis = NudgeMover.GetHeldAxis();
                        controls.text = NudgeMover.GetPrompt(axis) ?? NudgeMover.GetIdleControls();
                    }
                }

                if (hammer || bed)
                    ApplyLookIcons(look, item, hammer);
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("Nudge look text failed: " + e.Message);
            }
        }

        internal static void SetAltIconsPostfix(LookUI __instance)
        {
            try
            {
                if (__instance == null)
                    return;

                Traverse look = Traverse.Create(__instance);
                GoPointer pointer = look.Field("pointer").GetValue<GoPointer>();
                GoPointerButton button = pointer == null
                    ? null
                    : Traverse.Create(pointer).Field("pointedAtButton").GetValue<GoPointerButton>();
                bool hammer = NudgeMover.HoldingHammer(pointer);
                if (hammer || IsBed(button))
                    ApplyLookIcons(look, button != null ? button.GetComponent<ShipItem>() : null, hammer);
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("Nudge key hints failed: " + e.Message);
            }
        }

        private static bool IsBed(GoPointerButton button)
        {
            if (button == null)
                return false;
            if (button.GetComponent<ShipItemBed>() != null)
                return true;
            if (button.GetComponent<GPButtonBed>() != null)
                return true;
            Component logic = button.GetComponent("BedLogic");
            return logic != null;
        }

        // Furniture Fix blanks bed key labels. Restore each side only when that
        // side has an action and is in keyboard-icon mode so F/R are not drawn
        // on mouse icons or on right-only prompts (nailed Sleep, hammer lock).
        private static void ApplyLookIcons(Traverse look, ShipItem item, bool hammer)
        {
            if (!ResolveKeys() || look == null || GameState.lookingWithController)
                return;

            TextMesh controls = look.Field("controlsText").GetValue<TextMesh>();
            if (!hammer && item != null && item.nailed && controls != null
                && !string.IsNullOrEmpty(controls.text) && controls.text.IndexOf('\n') < 0)
            {
                controls.text = "\n" + controls.text;
            }

            bool hasLeft;
            bool hasRight;
            ReadLines(controls != null ? controls.text : "", out hasLeft, out hasRight);
            if (!hammer && item != null && item.nailed)
                hasLeft = false;

            bool alt = look.Field("altIconsOn").GetValue<bool>();
            if (hasLeft)
            {
                ApplySide(look, "mouseLIcon", "textLicon", _pickUp, alt);
                look.Method("ShowLicon").GetValue();
            }
            else
            {
                HideSide(look, "mouseLIcon", "textLicon");
            }

            if (hasRight)
            {
                ApplySide(look, "mouseRIcon", "textRIcon", _activate, alt);
                look.Method("ShowRicon").GetValue();
            }
            else
            {
                HideSide(look, "mouseRIcon", "textRIcon");
            }
        }

        private static void ReadLines(string text, out bool hasLeft, out bool hasRight)
        {
            if (string.IsNullOrEmpty(text))
            {
                hasLeft = false;
                hasRight = false;
                return;
            }

            int n = text.IndexOf('\n');
            if (n < 0)
            {
                hasLeft = false;
                hasRight = text.Trim().Length > 0;
                return;
            }

            hasLeft = text.Substring(0, n).Trim().Length > 0;
            hasRight = text.Substring(n + 1).Trim().Length > 0;
        }

        private static void HideSide(Traverse look, string mouseField, string textField)
        {
            Renderer mouse = look.Field(mouseField).GetValue<Renderer>();
            if (mouse != null)
                mouse.enabled = false;
            TextMesh text = look.Field(textField).GetValue<TextMesh>();
            if (text != null)
                text.gameObject.SetActive(false);
        }

        private static void ApplySide(
            Traverse look,
            string mouseField,
            string textField,
            object action,
            bool alt)
        {
            Renderer mouse = look.Field(mouseField).GetValue<Renderer>();
            TextMesh text = look.Field(textField).GetValue<TextMesh>();
            if (text == null)
                return;

            if (mouse != null && mouse.enabled)
            {
                text.text = "";
                return;
            }

            Stamp(look, textField, action, alt);
        }

        private static void Stamp(Traverse look, string textField, object action, bool alt)
        {
            if (!ResolveKeys() || action == null)
                return;

            TextMesh text = look.Field(textField).GetValue<TextMesh>();
            if (text == null)
                return;

            KeyCode key = ReadKey(action, alt);
            if (IsMouse(key) || key == KeyCode.None)
                key = ReadKey(action, !alt);
            if (IsMouse(key) || key == KeyCode.None)
                return;

            text.text = key.ToString();
        }

        private static bool ResolveKeys()
        {
            if (_resolvedKeys)
                return _getKeyCode != null;

            _resolvedKeys = true;
            Type gameInput = AccessTools.TypeByName("GameInput");
            Type inputName = AccessTools.TypeByName("InputName");
            if (gameInput == null || inputName == null || !inputName.IsEnum)
                return false;

            _getKeyCode = AccessTools.Method(gameInput, "GetKeyCode");
            if (_getKeyCode == null)
                return false;

            try
            {
                _pickUp = Enum.Parse(inputName, "PickUp");
                _activate = Enum.Parse(inputName, "Activate");
            }
            catch
            {
                _getKeyCode = null;
                return false;
            }

            return true;
        }

        private static KeyCode ReadKey(object action, bool input2)
        {
            try
            {
                object value = _getKeyCode.Invoke(null, new object[] { action, input2, false });
                if (value is KeyCode key)
                    return key;
            }
            catch
            {
            }

            return KeyCode.None;
        }

        private static bool IsMouse(KeyCode key)
        {
            return key == KeyCode.Mouse0 || key == KeyCode.Mouse1;
        }
    }
}
