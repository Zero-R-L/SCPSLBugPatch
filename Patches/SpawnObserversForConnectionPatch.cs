using HarmonyLib;
using Mirror;
using System.Linq;

namespace SCPSLBugPatch.Patches
{
    [HarmonyPatch(typeof(NetworkServer), nameof(NetworkServer.SpawnObserversForConnection))]
    internal class SpawnObserversForConnectionPatch
    {
        private static void AddLog(string content)
        {
            MainClass.AddLog("NetworkServer.SpawnObserversForConnection: " + content);
        }
        private static bool Prefix(NetworkConnectionToClient conn)
        {
            if (!conn.isReady)
            {
                return false;
            }
            conn.Send(default(ObjectSpawnStartedMessage));
            foreach (var keyValuePair in NetworkServer.spawned.ToList())
            {
                if (keyValuePair.Value == null)
                {
                    AddLog("value is null in NetworkServer.spawned.Values, REMOVING");
                    NetworkServer.spawned.Remove(keyValuePair.Key);
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
