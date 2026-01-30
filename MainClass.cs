using HarmonyLib;
using LabApi.Events.Handlers;
using LabApi.Features;
using LabApi.Features.Console;
using LabApi.Loader.Features.Plugins;
using MEC;
using Scp914;
using SCPSLBugPatch.Patches;
using System;
using System.Collections.Generic;
using System.IO;

namespace SCPSLBugPatch
{
    internal class MainClass : Plugin
    {
        private const string PluginName = "SCPSLBugPatch";
        private static string LogFilePath;
        private static Harmony harmony;
        private static HashSet<string>
        public override string Name => PluginName;
        public override string Author => "ZeroRL";
        public override Version Version => new(1, 1, 1);
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
            harmony = new($"{PluginName}-{DateTime.Now.Ticks}");
            harmony.PatchAll();
            ServerEvents.RoundRestarted += OnRestartingRound;
        }
        public override void Disable()
        {
            harmony.UnpatchAll(harmony.Id);
            ServerEvents.RoundRestarted -= OnRestartingRound;
        }
        private void OnRestartingRound()
        {
            BadDataLogSpamPatch.LogBadDataInfo();
            BadDataLogSpamPatch.Initialize();
        }
    }
}
