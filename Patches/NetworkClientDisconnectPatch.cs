using HarmonyLib;
using Mirror;

namespace SCPSLBugPatch.Patches
{
    [HarmonyPatch(typeof(NetworkClient), nameof(NetworkClient.Disconnect))]
    internal class NetworkClientDisconnectPatch
    {
        private static bool Prefix()
        {
            if (NetworkClient.connection == null)
            {
                return false;
            }
            if (!Shutdown._quitting)
            {
                if (NetworkClient.connection.connectionId == 0)
                {
                    MainClass.AddLog($"NetworkClient.Disconnect: NetworkClient.connection.connectionId is 0");
                    return false;
                }
            }
            if (NetworkClient.connectState == ConnectState.Connecting || NetworkClient.connectState == ConnectState.Connected)
            {
                NetworkClient.connectState = ConnectState.Disconnecting;
                NetworkClient.ready = false;
            }
            NetworkClient.connection.Disconnect();
            return false;
        }
    }
}
