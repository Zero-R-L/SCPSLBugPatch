using CommandSystem;
using SCPSLBugPatch.Patches;
using System;

namespace SCPSLBugPatch
{
    [CommandHandler(typeof(GameConsoleCommandHandler))]
    internal class ShowBadDataInfo : ICommand
    {
        public string Command => "ShowBadDataInfo";
        public string[] Aliases => new[] { "sbnw" };
        public string Description => "Show Bad Data Info (Maybe DDoS Data)";
        public bool Execute(ArraySegment<string> arguments, ICommandSender sender, out string response)
        {
            response = OnMessageReceivedPatch.GetBadDataInfo();
            return true;
        }
    }
}
