using System.Linq;
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

        public string[] HelpMessage(string command)
        {
            return new[] {""};
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
                            BotConfiguration.Current.Settings.MatchActions.Add(new ActionMatch
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
                            string matchString = segments[1];
                            ActionMatch match = BotConfiguration.Current.Settings.MatchActions.SingleOrDefault(
                                x => x.MatchString == matchString);
                            if (match == null)
                                return;

                            BotConfiguration.Current.Settings.MatchActions.Remove(match);
                            BotConfiguration.Current.SaveConfig<BotSettings>("config");
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