using HarmonyLib;
using PluginAPI.Core;
using PluginAPI.Core.Attributes;
using PluginAPI.Events;
using SCPSLBugPatch.Patches;
using System;
using System.IO;

namespace SCPSLBugPatch
{
    internal class MainClass
    {
        private const string PluginName = "SCPSLBugPatch";
        private static string LogFilePath;
        private static Harmony harmony;
        [PluginEntryPoint(PluginName, "1.0.0", PluginName, "ZeroRL")]
        private void LoadPlugin()
        {
            string folder = FileManager.GetAppFolder(true, false, "");
            if (Directory.Exists(folder))
            {
                LogFilePath = Path.Combine(folder, $"{PluginName}.log");
            }
            OnMessageReceivedPatch.Initialize();
            harmony = new Harmony($"{PluginName}-{DateTime.Now.Ticks}");
            harmony.PatchAll();
            EventManager.RegisterAllEvents(this);
            PluginHandler handler = PluginHandler.Get(this);
            Log.Info($"{handler.PluginName} v{handler.PluginVersion} by {handler.PluginAuthor} has been enabled!");
        }
        internal static void AddLog(string content)
        {
            Log.Info(content);
            if (LogFilePath != null)
            {
                File.AppendAllText(LogFilePath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff zzz}] [{Server.Port}] {content}\r\n");
            }
        }
        [PluginEvent]
        private void OnRoundRestart(RoundRestartEvent _)
        {
            OnMessageReceivedPatch.Initialize();
            OnMessageReceivedPatch.LogBadDataInfo();
        }
    }
}
