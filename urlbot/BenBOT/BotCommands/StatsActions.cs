using BenBOT.Configuration;
using Meebey.SmartIrc4net;

namespace BenBOT.BotCommands
{
    public class StatsActions : IBotCommand
    {
        public string[] CommandsHandled
        {
            get { return new[] {"!URLSTATS"}; }
        }

        public string[] HelpMessage(string command)
        {
            return new[] {""};
        }

        public void ProcessCommand(string[] segments, BotUser user, IrcClient irc, IrcMessageData senderData)
        {
            switch (segments[0].ToUpper())
            {
                case "!URLSTATS":
                    //var stats = from x in FilterURLs(segments)
                    //            group x by x.Channel
                    //                into xG
                    //                select new
                    //                {
                    //                    Channel = xG.Key,
                    //                    URLCount = xG.Count()
                    //                };
                    //irc.SendMessage(SendType.Message, e.Data.Nick,
                    //    "Current URL Cache Size: " + BotConfiguration.Current.MatchedURLs.Count());

                    //irc.SendMessage(SendType.Message, e.Data.Nick,
                    //    string.Format("{0,-20}{1,-10}", "Channel", "URL Count"));
                    //foreach (var item in stats)
                    //{
                    //    irc.SendMessage(SendType.Message, e.Data.Nick,
                    //        string.Format("{0,-20}{1,-10}", item.Channel, item.URLCount));
                    //}

                    break;
            }
        }
    }
}