﻿using Exiled.API.Features;
using HarmonyLib;
using Mirror;
using System;
using UnityEngine;
using static PlayerList;

namespace SCPSLBugPatch.Patches
{
    [HarmonyPatch(typeof(LocalConnectionToServer), nameof(LocalConnectionToServer.Disconnect))]
    internal class LocalConnectionToServerDisconnenctPatch
    {
        public static bool Prefix(LocalConnectionToServer __instance)
        {
            if (__instance.identity.isServer && !Shutdown._quitting)
            {
                MainClass.AddLog($"[LocalConnectionToServer-DISCONNECT] Avoid a crash {ServerShutdown.ShutdownState}");
                Shutdown.Quit();
                return false;
            }
            return true;
        }
    }
}
