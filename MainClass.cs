using HarmonyLib;
using LabApi.Events.CustomHandlers;
using LabApi.Events.Handlers;
using LabApi.Features.Console;
using LabApi.Loader.Features.Plugins;
using SCPSLBugPatch.Patches;
using System;
using System.IO;

namespace SCPSLBugPatch
{
    internal class MainClass :Plugin
    {
        private const string PluginName = "SCPSLBugPatch";
        private static string LogFilePath { get; set; }
        private static Harmony Harmony { get; } = new Harmony($"{PluginName}");

        public override string Name => PluginName;

        public override string Description => null;

        public override string Author => "ZeroRL";

        public override Version Version => new Version(1,0,0,0);

        public override Version RequiredApiVersion => new Version(1,0,2,0);
        internal static void AddLog(string content)
        {
            Logger.Info(content);
            if (LogFilePath != null)
            {
                File.AppendAllText(LogFilePath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff zzz}] [{ServerStatic.ServerPort}] {content}\r\n");
            }
        }
        private void OnRoundRestart()
        {
            OnMessageReceivedPatch.LogBadDataInfo();
            OnMessageReceivedPatch.Initialize();
        }

        public override void Enable()
        {

            string folder = FileManager.GetAppFolder();
            if (Directory.Exists(folder))
            {
                LogFilePath = Path.Combine(folder, $"{PluginName}.log");
            }
            OnMessageReceivedPatch.Initialize();
            Harmony.PatchAll();
            ServerEvents.RoundRestarted += OnRoundRestart;
        }

        public override void Disable()
        {
            Harmony.UnpatchAll();
        }
    }
}
