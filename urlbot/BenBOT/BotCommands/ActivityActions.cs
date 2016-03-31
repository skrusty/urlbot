using System.Linq;
using BenBOT.BotListeners;
using BenBOT.Configuration;
using Meebey.SmartIrc4net;

namespace BenBOT.BotCommands
{
    public class ActivityActions : IBotCommand
    {
        public string[] CommandsHandled
        {
            get { return new[] {"!LASTSEEN", "!SEEN", "!LASTSPOKE"}; }
        }

        public string HelpMessage(string command)
        {
            return "";
        }

        public void ProcessCommand(string[] segments, BotUser user, IrcClient irc, IrcMessageData senderData)
        {
            var uOI = BotConfiguration.Current.Settings.GetUser(segments[1]);
            if (uOI == null)
                return;

            switch (segments[0].ToUpper())
            {
                case "!LASTSEEN":
                case "!SEEN":
                    if (irc.GetIrcUser(segments[1]) != null)
                    {
                        var ircUser = irc.GetIrcUser(segments[1]);
                        var ourChans =
                            ircUser.JoinedChannels.ToList()
                                .Where(x => BotConfiguration.Current.Settings.AutoJoinChannels.Contains(x));
                        irc.SendMessage(SendType.Message, senderData.Nick,
                            string.Format("{0} is currently in {1}",
                                uOI.Nick, string.Join(", ", ourChans.ToArray())));
                    }
                    else if (uOI.Attributes.ContainsKey(ActivityListener.LastActionAttributeKey))
                    {
                        var lastAction = (UserActivityItem) uOI.Attributes[ActivityListener.LastActionAttributeKey];
                        irc.SendMessage(SendType.Message, senderData.Nick,
                            string.Format("{0} was last seen in {1} on {2} at {3}",
                                uOI.Nick, lastAction.Channel, lastAction.Date.Date.ToShortDateString(),
                                lastAction.Date.ToShortTimeString()));
                    }
                    else
                        irc.SendMessage(SendType.Message, senderData.Nick, "No idea, sorry.");
                    break;
                case "!LASTSPOKE":
                    if (uOI.Attributes.ContainsKey(ActivityListener.LastSpokeAttributeKey))
                    {
                        var lastSpoke = (UserActivityItem) uOI.Attributes[ActivityListener.LastSpokeAttributeKey];
                        irc.SendMessage(SendType.Message, senderData.Nick,
                            string.Format("{0} was last seen speaking in {1} on {2} at {3}",
                                uOI.Nick, lastSpoke.Channel, lastSpoke.Date.Date.ToShortDateString(),
                                lastSpoke.Date.ToShortTimeString()));
                    }
                    else
                        irc.SendMessage(SendType.Message, senderData.Nick, "No idea, sorry.");
                    break;
            }
        }
    }
}