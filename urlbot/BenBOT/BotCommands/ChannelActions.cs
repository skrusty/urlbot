using System;
using System.Linq;
using BenBOT.Configuration;
using Meebey.SmartIrc4net;

namespace BenBOT.BotCommands
{
    public class ChannelActions : IBotCommand
    {
        public string[] CommandsHandled
        {
            get { return new[] {"!JOIN", "!PART"}; }
        }

        public string HelpMessage(string command)
        {
            throw new NotImplementedException();
        }

        public void ProcessCommand(string[] segments, BotUser user, IrcClient irc, IrcMessageData senderData)
        {
            switch (segments[0].ToUpper())
            {
                case "!JOIN":
                    // Save new channel to join list
                    try
                    {
                        if (user.IsAdmin)
                        {
                            irc.RfcJoin(segments[1]);
                            BotConfiguration.Current.Settings.AutoJoinChannels.Add(segments[1]);

                            // Save Configuration
                            BotConfiguration.Current.SaveConfig<BotSettings>("config");

                            BotUser.BroadcastToAdmins(irc, "Joined channel {0} by {1}", segments[1], senderData.Nick);
                        }
                        else
                        {
                            BotUser.BroadcastToAdmins(irc,
                                "{0} attempted to force a channel join for channel {1} and is not an admin",
                                senderData.Nick, segments.Any() ? segments[1] : "<empty channel name>");
                        }
                    }
                    catch
                    {
                    }
                    break;
                case "!PART":
                    try
                    {
                        if (user.IsAdmin)
                        {
                            string partChan;
                            partChan = segments.Count() > 1 ? segments[1] : senderData.Channel;
                            irc.RfcPart(partChan);
                            BotConfiguration.Current.Settings.AutoJoinChannels.Remove(partChan);

                            BotUser.BroadcastToAdmins(irc, "Parted channel {0} by {1}", segments[1], senderData.Nick);
                        }
                        else
                        {
                            BotUser.BroadcastToAdmins(irc,
                                "{0} attempted to force a channel part for channel {1} and is not an admin",
                                senderData.Nick, segments.Any() ? segments[1] : "<empty channel name>");
                        }
                    }
                    catch
                    {
                    }
                    break;
            }
        }
    }
}