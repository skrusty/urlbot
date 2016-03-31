using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BenBOT.Configuration;
using Meebey.SmartIrc4net;

namespace BenBOT.BotCommands
{
    public class CoreActions : IBotCommand
    {
        public string[] CommandsHandled { get { return new[] { "!HELP" }; } }
        public string HelpMessage(string command)
        {
            return "!Help [Command]";
        }

        public void ProcessCommand(string[] segments, BotUser user, IrcClient irc, IrcMessageData senderData)
        {
            switch (segments[0].ToUpper())
            {
                case "!HELP":
                    if (segments.Count() > 1)
                    {
                        irc.SendMessage(SendType.Message, senderData.Nick,
                            BotModulesManager.GetBotCommandHelp(segments[1]));
                    }
                    else
                    {
                        // return help for all commands
                        foreach (var cmd in BotModulesManager.BotCommands)
                        {
                            foreach(var cmdItem in cmd.CommandsHandled)
                            {
                                var helpStrting = cmd.HelpMessage(cmdItem);
                                if (string.IsNullOrEmpty(helpStrting))
                                    helpStrting = $"{cmdItem} provides no help";
                                irc.SendMessage(SendType.Message, senderData.Nick,
                                    helpStrting);
                            }
                        }
                    }
                break;
            }
        }
    }
}
