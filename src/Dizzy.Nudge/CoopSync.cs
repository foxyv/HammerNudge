using System;
using System.Reflection;
using BepInEx.Bootstrap;
using HarmonyLib;

namespace Dizzy.Nudge
{
    // SailwindCoop syncs items on events (pick up, drop, nail), not by streaming
    // positions, so a nudge never reached the other players and a guest's nudge
    // never reached the host's save. Its drop message carries an item's pose on
    // its boat and leaves the nailed state alone, so send one after each nudge.
    // Reflection only: SailwindCoop is optional and resolved on first use.
    internal static class CoopSync
    {
        internal const string CoopGuid = "com.sailwindcoop.mod";

        private static bool _resolved;
        private static bool _warned;
        private static PropertyInfo _isMultiplayer;
        private static PropertyInfo _instance;
        private static PropertyInfo _isApplyingRemoteState;
        private static MethodInfo _onLocalDrop;

        internal static void BroadcastPose(ShipItem item)
        {
            if (item == null)
                return;

            try
            {
                if (!Resolve())
                    return;
                if (!(bool)_isMultiplayer.GetValue(null, null))
                    return;

                object manager = _instance.GetValue(null, null);
                if (manager == null)
                    return;
                if (_isApplyingRemoteState != null && (bool)_isApplyingRemoteState.GetValue(manager, null))
                    return;

                _onLocalDrop.Invoke(manager, new object[] { item });
            }
            catch (Exception e)
            {
                WarnOnce("Nudge: co-op sync failed: " + (e.InnerException ?? e).Message);
            }
        }

        private static bool Resolve()
        {
            if (_resolved)
                return _onLocalDrop != null;
            _resolved = true;

            if (!Chainloader.PluginInfos.ContainsKey(CoopGuid))
                return false;

            Type plugin = AccessTools.TypeByName("SailwindCoop.Plugin");
            Type manager = AccessTools.TypeByName("SailwindCoop.Sync.ItemSyncManager");
            if (plugin != null && manager != null)
            {
                _isMultiplayer = AccessTools.Property(plugin, "IsMultiplayer");
                _instance = AccessTools.Property(manager, "Instance");
                _isApplyingRemoteState = AccessTools.Property(manager, "IsApplyingRemoteState");
                if (_isMultiplayer != null && _instance != null)
                    _onLocalDrop = AccessTools.Method(manager, "OnLocalDrop", new[] { typeof(ShipItem) });
            }

            if (_onLocalDrop == null)
                WarnOnce("Nudge: SailwindCoop item sync not found; nudges will not reach other players.");
            return _onLocalDrop != null;
        }

        private static void WarnOnce(string message)
        {
            if (_warned)
                return;
            _warned = true;
            Plugin.Log.LogWarning(message);
        }
    }
}
