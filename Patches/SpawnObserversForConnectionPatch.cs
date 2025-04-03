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
            int a = 0;
            try
            {
                a++;
                if (conn is null)
                {
                    AddLog("NetworkConnectionToClient is null");
                    return false;
                }

                a++;
                if (!NetworkServer.initialized)
                {
                    AddLog("NetworkServer is not initialized");
                    return false;
                }

                a++;
                if (!conn.isReady)
                {
                    return false;
                }

                conn.Send(default(ObjectSpawnStartedMessage));

                a++;
                if (NetworkServer.spawned is null)
                {
                    AddLog("NetworkServer.spawned is null");
                    return false;
                }

                a++;
                foreach (NetworkIdentity value in NetworkServer.spawned.Values)
                {
                    if (value is null)
                    {
                        AddLog("value is null in NetworkServer.spawned.Values");
                        return false;
                    }

                    if (value.gameObject is null)
                    {
                        AddLog("value.gameObject is null in NetworkServer.spawned.Values");
                        return false;
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
            catch (System.Exception e)
            {
                AddLog($"[{a}] " + e.ToString());
                return false;
            }
        }
    }
}
