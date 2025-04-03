using CommandSystem.Commands.Shared;
using HarmonyLib;

namespace SCPSLBugPatch.Patches
{
    [HarmonyPatch(typeof(ServerShutdown), nameof(ServerShutdown.Shutdown))]
    internal class ServerShutdownPatch
    {
        private static void Prefix()
        {
            OnMessageReceivedPatch.LogBadDataInfo();
        }
    }
}
