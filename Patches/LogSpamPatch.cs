using HarmonyLib;
using Mirror;
using NetworkManagerUtils.Dummies;
using PlayerRoles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SCPSLBugPatch.Patches
{
    [HarmonyPatch(typeof(NetworkServer), nameof(NetworkServer.BroadcastToConnection))]
    internal static class LogSpamPatch
    {
        private static bool Prefix(NetworkConnectionToClient connection)
        {
            foreach (NetworkIdentity item in connection.observing)
            {
                if (item != null)
                {
                    NetworkWriter networkWriter = NetworkServer.SerializeForConnection(item, connection);
                    if (networkWriter != null)
                    {
                        EntityStateMessage message = new()
                        {
                            netId = item.netId,
                            payload = networkWriter.ToArraySegment()
                        };
                        connection.Send(message);
                    }
                }
                else
                {
                    //Debug.LogWarning($"Found 'null' entry in observing list for connectionId={connection.connectionId}. Please call NetworkServer.Destroy to destroy networked objects. Don't use GameObject.Destroy.");
                }
            }
            return false;
        }
    }
}
