using HarmonyLib;
using Mirror;

namespace SCPSLBugPatch.Patches
{
    internal class BroadcastToConnectionPatch
    {
        [HarmonyPatch(typeof(NetworkServer), nameof(NetworkServer.BroadcastToConnection))]
        private static bool Prefix(NetworkConnectionToClient connection)
        {
            foreach (NetworkIdentity item in connection.observing)
            {
                if (item != null)
                {
                    NetworkWriter networkWriter = NetworkServer.SerializeForConnection(item, connection);
                    if (networkWriter != null)
                    {
                        EntityStateMessage entityStateMessage = default;
                        entityStateMessage.netId = item.netId;
                        entityStateMessage.payload = networkWriter.ToArraySegment();
                        EntityStateMessage message = entityStateMessage;
                        connection.Send(message);
                    }
                }
                //else
                //{
                //    Debug.LogWarning($"Found 'null' entry in observing list for connectionId={connection.connectionId}. Please call NetworkServer.Destroy to destroy networked objects. Don't use GameObject.Destroy.");
                //}
            }
            return false;
        }
    }
}
