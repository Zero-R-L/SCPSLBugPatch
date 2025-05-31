using HarmonyLib;
using NetworkManagerUtils.Dummies;
using PlayerRoles;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SCPSLBugPatch.Patches
{
    [HarmonyPatch(typeof(PlayerRoleManager), nameof(PlayerRoleManager.PopulateDummyActions))]
    internal class PopulateDummyActionsPatch
    {
        private static bool Prefix(PlayerRoleManager __instance, Action<DummyAction> actionAdder, Action<string> categoryAdder)
        {
            if (__instance._dummyProviders == null || actionAdder == null || categoryAdder == null)
            {
                return false;
            }
            return true;
        }
    }
}
