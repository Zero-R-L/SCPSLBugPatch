using HarmonyLib;
using Mirror;

namespace SCPSLBugPatch.Patches
{
    [HarmonyPatch(typeof(NetworkServer), nameof(NetworkServer.SpawnObserversForConnection))]
    internal class SpawnObserversForConnectionPatch
    {
        private static void AddLog(string content)
        {
            Plugin.AddLog("NetworkServer.SpawnObserversForConnection: " + content);
        }
        private static bool Prefix(NetworkConnectionToClient conn)
        {
            if (conn == null)
            {
                AddLog("NetworkConnectionToClient is null");
                return false;
            }
            if (!NetworkServer.initialized)
            {
                AddLog("NetworkServer is not initialized");
                return false;
            }
            if (!conn.isReady)
            {
                return false;
            }
            conn.Send(default(ObjectSpawnStartedMessage));
            if (NetworkServer.spawned == null)
            {
                AddLog("NetworkServer.spawned is null");
                return false;
            }
            foreach (NetworkIdentity value in NetworkServer.spawned.Values)
            {
                if (value == null)
                {
                    AddLog("value is null in NetworkServer.spawned.Values");
                    continue;
                }
                if (value.gameObject == null)
                {
                    AddLog("value.gameObject is null in NetworkServer.spawned.Values");
                    continue;
                }
                if (!value.gameObject.activeSelf)
                {
                    continue;
                }
                if (value.visible == Visibility.ForceShown)
                {
                    value.AddObserver(conn);
                }
                else
                {
                    if (value.visible == Visibility.ForceHidden || value.visible != 0)
                    {
                        continue;
                    }

                    if (NetworkServer.aoi != null)
                    {
                        if (NetworkServer.aoi.OnCheckObserver(value, conn))
                        {
                            value.AddObserver(conn);
                        }
                    }
                    else
                    {
                        value.AddObserver(conn);
                    }
                }
            }
            conn.Send(default(ObjectSpawnFinishedMessage));
            return false;
        }
    }
}
