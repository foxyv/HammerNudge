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
        private static string _gameControls;
        private static string _nudgeControls;
        private static TextMesh _keysText;
        private static GoPointerButton _controlsButton;
        private static string _shownKeys;
        private static int _shownLines;

        internal static void ShowLookTextPostfix(LookUI __instance, GoPointerButton button)
        {
            try
            {
                if (__instance == null || button == null)
                    return;
                if (NudgeConfig.Enabled == null || !NudgeConfig.Enabled.Value)
                {
                    HideKeys();
                    return;
                }

                Traverse look = Traverse.Create(__instance);
                GoPointer pointer = look.Field("pointer").GetValue<GoPointer>();
                if (pointer == null)
                    pointer = NudgeMover.GetLookingPointer(button);

                bool hammer = NudgeMover.HoldingHammer(pointer);
                bool bed = IsBed(button);
                ShipItem item = button.GetComponent<ShipItem>();
                bool target = hammer && NudgeMover.IsNudgeTarget(item);
                string prompt = target ? NudgeMover.GetPrompt(NudgeMover.GetHeldAxis()) : null;

                TextMesh controls = look.Field("controlsText").GetValue<TextMesh>();
                string shown = null;
                if (target && controls != null)
                {
                    shown = BuildControls(button, controls.text, prompt);
                    controls.text = shown;
                }

                if (hammer || bed)
                    ApplyLookIcons(look, item, hammer);

                if (target && prompt == null && controls != null)
                    ShowKeys(controls, shown, NudgeMover.GetIdleControls());
                else
                    HideKeys();
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("Nudge look text failed: " + e.Message);
            }
        }

        internal static void ClearTextPostfix()
        {
            try
            {
                HideKeys();
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("Nudge key line hide failed: " + e.Message);
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

        private static string BuildControls(GoPointerButton button, string shown, string prompt)
        {
            if (shown == _nudgeControls)
                shown = button == _controlsButton ? _gameControls : "";
            _controlsButton = button;
            _gameControls = shown;

            if (prompt == null)
            {
                _nudgeControls = null;
                return shown;
            }

            string above;
            string clicks;
            SplitClickLines(shown, out above, out clicks);
            _nudgeControls = above.Length == 0 ? prompt : above + "\n" + prompt;
            return _nudgeControls;
        }

        private static void ShowKeys(TextMesh controls, string shown, string keys)
        {
            TextMesh text = GetKeysText(controls);
            int lines = Math.Max(2, CountLines(shown));
            if (keys != _shownKeys || lines != _shownLines)
            {
                _shownKeys = keys;
                _shownLines = lines;
                text.text = keys + new string('\n', lines);
            }

            if (!text.gameObject.activeSelf)
                text.gameObject.SetActive(true);
        }

        private static void HideKeys()
        {
            if (_keysText != null && _keysText.gameObject.activeSelf)
                _keysText.gameObject.SetActive(false);
        }

        private static TextMesh GetKeysText(TextMesh controls)
        {
            if (_keysText != null && _keysText.transform.parent == controls.transform)
                return _keysText;

            if (_keysText != null)
                UnityEngine.Object.Destroy(_keysText.gameObject);

            _keysText = UnityEngine.Object.Instantiate(controls, controls.transform, false);
            _keysText.name = "nudge keys text";
            _shownKeys = null;
            Transform keys = _keysText.transform;
            for (int i = keys.childCount - 1; i >= 0; i--)
            {
                GameObject child = keys.GetChild(i).gameObject;
                child.SetActive(false);
                UnityEngine.Object.Destroy(child);
            }

            keys.localPosition = Vector3.zero;
            keys.localRotation = Quaternion.identity;
            keys.localScale = Vector3.one;
            return _keysText;
        }

        private static int CountLines(string text)
        {
            if (string.IsNullOrEmpty(text))
                return 1;

            int lines = 1;
            foreach (char c in text)
            {
                if (c == '\n')
                    lines++;
            }

            return lines;
        }

        private static void SplitClickLines(string text, out string above, out string clicks)
        {
            text = text ?? "";
            int right = text.LastIndexOf('\n');
            if (right < 0)
            {
                above = "";
                clicks = "\n" + text;
                return;
            }

            int left = right == 0 ? -1 : text.LastIndexOf('\n', right - 1);
            above = left < 0 ? "" : text.Substring(0, left);
            clicks = text.Substring(left + 1);
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
                HideSide(look, "textLicon");
            }

            if (hasRight)
            {
                ApplySide(look, "mouseRIcon", "textRIcon", _activate, alt);
                look.Method("ShowRicon").GetValue();
            }
            else
            {
                HideSide(look, "textRIcon");
            }
        }

        private static void ReadLines(string text, out bool hasLeft, out bool hasRight)
        {
            string above;
            string clicks;
            SplitClickLines(text, out above, out clicks);
            int n = clicks.IndexOf('\n');
            hasLeft = clicks.Substring(0, n).Trim().Length > 0;
            hasRight = clicks.Substring(n + 1).Trim().Length > 0;
        }

        private static void HideSide(Traverse look, string textField)
        {
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
