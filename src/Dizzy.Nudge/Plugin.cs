using BepInEx;
using BepInEx.Logging;
using Dizzy.Nudge.Patches;
using HarmonyLib;

namespace Dizzy.Nudge
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    [BepInProcess("Sailwind.exe")]
    [BepInDependency("DogEggz.unlimitedhammer", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.nandbrew.furniturefix", BepInDependency.DependencyFlags.SoftDependency)]
    public class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "com.dizzy.sailwind.nudge";
        public const string PluginName = "Dizzy Nudge";
        public const string PluginVersion = "0.1.1";

        internal static ManualLogSource Log;
        internal static Plugin Instance;

        private Harmony _harmony;

        private void Awake()
        {
            Instance = this;
            Log = Logger;

            NudgeConfig.Bind(Config);

            _harmony = new Harmony(PluginGuid);
            PatchApplier.Apply(_harmony);

            Log.LogInfo($"{PluginName} v{PluginVersion} loaded (Enabled={NudgeConfig.Enabled.Value}, StepMeters={NudgeConfig.StepMeters.Value}).");
        }

        private void OnDestroy()
        {
            _harmony?.UnpatchSelf();

            if (Instance == this)
                Instance = null;
        }
    }
}
