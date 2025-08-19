using HarmonyLib;
using Mirror;
using RoundRestarting;
using System.Linq;

namespace SCPSLBugPatch.Patches
{
    internal static class DisconnectHostPatch
    {
        [HarmonyPatch(typeof(LocalConnectionToServer), nameof(LocalConnectionToServer.Disconnect))]
        private static class A
        {
            private static bool Prefix(LocalConnectionToServer __instance)
            {
                if (__instance.identity.isLocalPlayer && !Shutdown._quitting)
                {
                    MainClass.AddLog($"Preventing server from disconnecting from host connection. ShutdownState: {ServerShutdown.ShutdownState}");
                    return false;
                }
                return true;
            }
        }
        [HarmonyPatch(typeof(ReferenceHub), nameof(ReferenceHub.OnDestroy))]
        internal static class B
        {
            private static bool Prefix(ReferenceHub __instance)
            {
                if (__instance.isLocalPlayer && !RoundRestart.IsRoundRestarting)
                {
                    MainClass.AddLog($"Preventing server from disconnecting from host instance.");
                    return false;
                }
                return true;
            }
        }
        [HarmonyPatch(typeof(NetworkServer), nameof(NetworkServer.SpawnObserversForConnection))]
        private static class C
        {
            private static bool Prefix(NetworkConnectionToClient conn)
            {
                if (!conn.isReady)
                {
                    return false;
                }
                conn.Send(default(ObjectSpawnStartedMessage));
                foreach (System.Collections.Generic.KeyValuePair<uint, NetworkIdentity> keyValuePair in NetworkServer.spawned.ToList())
                {
                    if (keyValuePair.Value == null)
                    {
                        MainClass.AddLog($"NetworkServer.spawned[{keyValuePair.Key}] is null, REMOVING");
                        _ = NetworkServer.spawned.Remove(keyValuePair.Key);
                        continue;
                    }
                    if (!keyValuePair.Value.gameObject.activeSelf)
                    {
                        continue;
                    }
                    if (keyValuePair.Value.visible == Visibility.ForceShown)
                    {
                        keyValuePair.Value.AddObserver(conn);
                    }
                    else
                    {
                        if (keyValuePair.Value.visible == Visibility.ForceHidden || keyValuePair.Value.visible != 0)
                        {
                            continue;
                        }

                        if (NetworkServer.aoi != null)
                        {
                            if (NetworkServer.aoi.OnCheckObserver(keyValuePair.Value, conn))
                            {
                                keyValuePair.Value.AddObserver(conn);
                            }
                        }
                        else
                        {
                            keyValuePair.Value.AddObserver(conn);
                        }
                    }
                }
                conn.Send(default(ObjectSpawnFinishedMessage));
                return false;
            }
        }
    }
}
