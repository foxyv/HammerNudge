using BepInEx.Configuration;
using UnityEngine;

namespace Dizzy.Nudge
{
    internal static class NudgeConfig
    {
        internal static ConfigEntry<bool> Enabled;
        internal static ConfigEntry<float> StepMeters;
        internal static ConfigEntry<float> FineStepMeters;
        internal static ConfigEntry<KeyboardShortcut> FineModifier;
        internal static ConfigEntry<KeyboardShortcut> AwayKey;
        internal static ConfigEntry<KeyboardShortcut> VerticalKey;
        internal static ConfigEntry<KeyboardShortcut> StrafeKey;

        internal static void Bind(ConfigFile config)
        {
            Enabled = config.Bind(
                "Nudge",
                "Enabled",
                true,
                "Hold Q/T/E and click while holding a hammer to micro-move locked (nailed) furniture. Vanilla lock/unlock is unchanged when those keys are not held.");

            StepMeters = config.Bind(
                "Nudge",
                "StepMeters",
                0.05f,
                new ConfigDescription(
                    "How far each tap moves a locked item, in meters (1 game unit = 1 m).",
                    new AcceptableValueRange<float>(0.005f, 0.5f)));

            FineStepMeters = config.Bind(
                "Nudge",
                "FineStepMeters",
                0.01f,
                new ConfigDescription(
                    "Step size while the fine modifier is held.",
                    new AcceptableValueRange<float>(0.001f, 0.25f)));

            FineModifier = config.Bind(
                "Nudge",
                "FineModifier",
                new KeyboardShortcut(KeyCode.LeftShift),
                "Hold with a nudge key for FineStepMeters. Left or right Shift both count when this is a Shift key.");

            AwayKey = config.Bind(
                "Hotkeys",
                "AwayKey",
                new KeyboardShortcut(KeyCode.Q),
                "Hold and left-click to nudge away, right-click to nudge closer.");

            VerticalKey = config.Bind(
                "Hotkeys",
                "VerticalKey",
                new KeyboardShortcut(KeyCode.T),
                "Hold and left-click to nudge up, right-click to nudge down.");

            StrafeKey = config.Bind(
                "Hotkeys",
                "StrafeKey",
                new KeyboardShortcut(KeyCode.E),
                "Hold and left-click to nudge left, right-click to nudge right.");
        }
    }
}
