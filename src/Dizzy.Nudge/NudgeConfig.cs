using BepInEx.Configuration;
using UnityEngine;

namespace Dizzy.Nudge
{
    internal static class NudgeConfig
    {
        internal static ConfigEntry<bool> Enabled;
        internal static ConfigEntry<float> StepMeters;
        internal static ConfigEntry<float> FineStepMeters;
        internal static ConfigEntry<float> StepDegrees;
        internal static ConfigEntry<float> FineStepDegrees;
        internal static ConfigEntry<KeyboardShortcut> FineModifier;
        internal static ConfigEntry<KeyboardShortcut> RotateModifier;
        internal static ConfigEntry<KeyboardShortcut> AwayKey;
        internal static ConfigEntry<KeyboardShortcut> VerticalKey;
        internal static ConfigEntry<KeyboardShortcut> StrafeKey;
        internal static ConfigEntry<KeyboardShortcut> LevelKey;

        internal static void Bind(ConfigFile config)
        {
            Enabled = config.Bind(
                "Nudge",
                "Enabled",
                true,
                "Hold Q/T/E and click while holding a hammer to micro-move locked (nailed) furniture. Hold Alt for rotate. G levels the item to the deck. Vanilla lock/unlock is unchanged when those keys are not held.");

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
                    "Move step size while the fine modifier is held.",
                    new AcceptableValueRange<float>(0.001f, 0.25f)));

            StepDegrees = config.Bind(
                "Nudge",
                "StepDegrees",
                5f,
                new ConfigDescription(
                    "How far each tap rotates a locked item, in degrees.",
                    new AcceptableValueRange<float>(0.5f, 45f)));

            FineStepDegrees = config.Bind(
                "Nudge",
                "FineStepDegrees",
                1f,
                new ConfigDescription(
                    "Rotate step size while the fine modifier is held.",
                    new AcceptableValueRange<float>(0.1f, 15f)));

            FineModifier = config.Bind(
                "Nudge",
                "FineModifier",
                new KeyboardShortcut(KeyCode.LeftShift),
                "Hold with a nudge key for FineStepMeters / FineStepDegrees. Left or right Shift both count when this is a Shift key.");

            RotateModifier = config.Bind(
                "Nudge",
                "RotateModifier",
                new KeyboardShortcut(KeyCode.LeftAlt),
                "Hold with Q/T/E and click to rotate instead of move. Left or right Alt both count when this is an Alt key. Empty-hand furniture pickup (Furniture Fix) still uses Alt.");

            AwayKey = config.Bind(
                "Hotkeys",
                "AwayKey",
                new KeyboardShortcut(KeyCode.Q),
                "Hold and left-click to nudge away, right-click to nudge closer. With RotateModifier: tilt away / closer.");

            VerticalKey = config.Bind(
                "Hotkeys",
                "VerticalKey",
                new KeyboardShortcut(KeyCode.T),
                "Hold and left-click to nudge up, right-click to nudge down. With RotateModifier: roll left / right.");

            StrafeKey = config.Bind(
                "Hotkeys",
                "StrafeKey",
                new KeyboardShortcut(KeyCode.E),
                "Hold and left-click to nudge left, right-click to nudge right. With RotateModifier: turn left / right.");

            LevelKey = config.Bind(
                "Hotkeys",
                "LevelKey",
                new KeyboardShortcut(KeyCode.G),
                "While holding a hammer and looking at a locked item, press to flatten it to the deck (keep yaw).");
        }
    }
}
