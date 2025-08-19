using HarmonyLib;
using NetworkManagerUtils.Dummies;
using PlayerRoles;
using System;

namespace SCPSLBugPatch.Patches
{
    [HarmonyPatch(typeof(PlayerRoleManager), nameof(PlayerRoleManager.PopulateDummyActions))]
    internal static class DummyActionsErrorPatch
    {
        private static bool Prefix(Action<DummyAction> actionAdder, Action<string> categoryAdder)
        {
            return actionAdder != null && categoryAdder != null;
        }
    }
}
