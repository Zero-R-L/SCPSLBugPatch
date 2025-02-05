using HarmonyLib;
using Mirror;

namespace SCPSLBugPatch.Patches
{
    [HarmonyPatch(typeof(NetworkClient), nameof(NetworkClient.Disconnect))]
    internal class NetworkClientDisconnectPatch
    {
        private static bool Prefix()
        {
            if (NetworkClient.connectState == ConnectState.Connecting || NetworkClient.connectState == ConnectState.Connected)
            {
                NetworkClient.connectState = ConnectState.Disconnecting;
                NetworkClient.ready = false;
            }
            if (NetworkClient.connection is null)
            {
                return false;
            }
            if (NetworkClient.connection.connectionId is 0)
            {
                if (ServerShutdown.ShutdownState != ServerShutdown.ServerShutdownState.BroadcastingShutdown)
                {
                    MainClass.AddLog("NetworkClient.Disconnect: NetworkClient.connection.connectionId is 0");
                }
                return false;
            }
            NetworkClient.connection.Disconnect();
            return false;
        }
    }
}
