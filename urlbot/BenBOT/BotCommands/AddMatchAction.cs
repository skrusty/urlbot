﻿using System.Collections.Generic;
using System.Linq;
using BenBOT.BotListeners;
using BenBOT.Configuration;
using BenBOT.Models;
using Meebey.SmartIrc4net;

namespace BenBOT.BotCommands
{
    public class AddMatchAction : IBotCommand
    {
        public string[] CommandsHandled
        {
            get { return new[] {"!ADDMATCH", "!DELMATCH"}; }
        }

        public string HelpMessage(string command)
        {
            switch (command.ToUpper())
            {
                case "!ADDMATCH":
                    return "!ADDMATCH <Action(KICK|REPLY)> <MatchString> <Reason/Response>. (E.g. !ADDMATCH REPLY ?c# http://www.tutorialspoint.com/csharp/";
                case "!DELMATCH":
                    return "!DELMATCH <MatchString>";
            }
            throw new System.Exception("Unknown Command");
        }

        public void ProcessCommand(string[] segments, BotUser user, IrcClient irc, IrcMessageData senderData)
        {
            switch (segments[0].ToUpper())
            {
                case "!ADDMATCH":
                    if (user.IsAdmin || user.IsModerator)
                    {
                        try
                        {
                            BotConfiguration.Current.Config<List<ActionMatch>>(MatchListener.ConfigName)
                                .Add(new ActionMatch
                                {
                                    Action = segments[1],
                                    MatchString = segments[2],
                                    Reason = string.Join(" ", segments.Skip(3))
                                });
                            BotConfiguration.Current.SaveConfig<BotSettings>("config");
                        }
                        catch
                        {
                        }
                    }
                    break;
                case "!DELMATCH":
                    if (user.IsAdmin || user.IsModerator)
                    {
                        try
                        {
                            var matchString = segments[1];
                            var match = BotConfiguration.Current.Config<List<ActionMatch>>(MatchListener.ConfigName)
                                .SingleOrDefault(
                                    x => x.MatchString == matchString);
                            if (match == null)
                                return;

                            BotConfiguration.Current.Config<List<ActionMatch>>(MatchListener.ConfigName).Remove(match);
                            BotConfiguration.Current.SaveConfig<List<ActionMatch>>(MatchListener.ConfigName);
                        }
                        catch
                        {
                        }
                    }
                    break;
            }
        }
    }
}