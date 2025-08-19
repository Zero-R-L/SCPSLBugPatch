using HarmonyLib;
using LabApi.Events.Handlers;
using LabApi.Features;
using LabApi.Features.Console;
using LabApi.Loader.Features.Plugins;
using MEC;
using Scp914;
using SCPSLBugPatch.Patches;
using System;
using System.IO;

namespace SCPSLBugPatch
{
    internal class MainClass : Plugin
    {
        private const string PluginName = "SCPSLBugPatch";
        private static string LogFilePath { get; set; }
        private static CoroutineHandle CoroutineHandle { get; set; }
        private static Harmony Harmony { get; } = new Harmony($"{PluginName}-{DateTime.Now.Ticks}");
        public override string Name => PluginName;
        public override string Author => "ZeroRL";
        public override Version Version => new Version(1, 1, 0);
        public override string Description => PluginName;
        public override Version RequiredApiVersion => LabApiProperties.CurrentVersion;
        internal static void AddLog(string content)
        {
            Logger.Info(content);
            File.AppendAllText(LogFilePath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff zzz}] [{ServerStatic.ServerPort}] {content}\r\n");
        }
        public override void Enable()
        {
            string folder = FileManager.GetAppFolder();
            LogFilePath = Path.Combine(folder, $"{PluginName}.log");
            BadDataLogSpamPatch.Initialize();
            Harmony.PatchAll();
            ServerEvents.RoundRestarted += OnRestartingRound;
        }
        public override void Disable()
        {
            Harmony.UnpatchAll();
            ServerEvents.RoundRestarted -= OnRestartingRound;
        }
        private void OnRestartingRound()
        {
            BadDataLogSpamPatch.LogBadDataInfo();
            BadDataLogSpamPatch.Initialize();
        }
    }
}
