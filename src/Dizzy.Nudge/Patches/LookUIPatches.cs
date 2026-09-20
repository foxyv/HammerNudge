using System;
using HarmonyLib;
using UnityEngine;

namespace Dizzy.Nudge.Patches
{
    internal static class LookUIPatches
    {
        internal static void ShowLookTextPostfix(LookUI __instance, GoPointerButton button, TextMesh ___controlsText)
        {
            try
            {
                if (___controlsText == null || button == null)
                    return;
                if (NudgeConfig.Enabled == null || !NudgeConfig.Enabled.Value)
                    return;
                if (!Settings.controlsTextEnabled)
                    return;

                ShipItem item = button.GetComponent<ShipItem>();
                if (!NudgeMover.IsNudgeTarget(item))
                    return;

                GoPointer pointer = NudgeMover.GetLookingPointer(button);
                if (!NudgeMover.HoldingHammer(pointer))
                    return;

                NudgeAxis axis = NudgeMover.GetHeldAxis();
                string prompt = NudgeMover.GetPrompt(axis);
                if (string.IsNullOrEmpty(prompt))
                    return;

                ___controlsText.text = prompt;
                Traverse.Create(__instance).Method("ShowLicon").GetValue();
                Traverse.Create(__instance).Method("ShowRicon").GetValue();
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("Nudge look text failed: " + e.Message);
            }
        }
    }
}
