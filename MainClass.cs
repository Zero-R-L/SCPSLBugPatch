using Exiled.API.Features;
using HarmonyLib;
using SCPSLBugPatch.Patches;
using System;
using System.IO;

namespace SCPSLBugPatch
{
    internal class MainClass : Plugin<Config>
    {
        private const string PluginName = "SCPSLBugPatch";
        private static string LogFilePath { get; set; }
        private static Harmony Harmony { get; } = new Harmony(PluginName);
        public override string Name => PluginName;
        public override string Author => "ZeroRL";
        internal static void AddLog(string content)
        {
            Log.Info(content);
            File.AppendAllText(LogFilePath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff zzz}] [{ServerStatic.ServerPort}] {content}\r\n");
        }
        public override void OnEnabled()
        {
            string folder = FileManager.GetAppFolder();
            LogFilePath = Path.Combine(folder, $"{PluginName}.log");
            OnMessageReceivedPatch.Initialize();
            Harmony.PatchAll();
            Exiled.Events.Handlers.Server.RestartingRound += OnRestartingRound;
        }
        public override void OnDisabled()
        {
            Harmony.UnpatchAll();
        }
        private void OnRestartingRound()
        {
            OnMessageReceivedPatch.LogBadDataInfo();
            OnMessageReceivedPatch.Initialize();
        }
    }
}
