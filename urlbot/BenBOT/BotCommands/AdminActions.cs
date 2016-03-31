using System;
using System.Collections.Generic;
using System.Linq;
using BenBOT.Configuration;
using Meebey.SmartIrc4net;

namespace BenBOT.BotCommands
{
    public class AdminActions : IBotCommand
    {
        public string[] CommandsHandled
        {
            get { return new[] {"!CHANS", "!HISTORY", "!DUMP", "!SETUP", "!SETPERM", "!RELOAD", "!SAVE", "!QUIT"}; }
        }

        public string HelpMessage(string command)
        {
            return "";
        }

        public void ProcessCommand(string[] segments, BotUser user, IrcClient irc, IrcMessageData senderData)
        {
            switch (segments[0].ToUpper())
            {
                case "!CHANS":
                    try
                    {
                        if (user.IsAdmin)
                        {
                            irc.SendMessage(SendType.Message, senderData.Nick, "Active Channel List:");
                            foreach (var chan in irc.GetChannels())
                            {
                                irc.SendMessage(SendType.Message, senderData.Nick, chan);
                            }
                        }
                    }
                    catch
                    {
                    }
                    break;
                case "!HISTORY":
                    try
                    {
                        if (user.IsAdmin)
                        {
                            if (segments.Count() > 1)
                            {
                                var history = new List<HistoryItem>();
                                var histNick = segments[1];
                                var botUser =
                                    BotConfiguration.Current.Settings.KnownUsers.SingleOrDefault(x => x.Nick == histNick);
                                if (botUser != null)
                                    history = botUser.CommandHistory.Take(10).ToList();

                                irc.SendMessage(SendType.Message, senderData.Nick, "Command History List (last 10):");
                                history.ToList().ForEach(x =>
                                    irc.SendMessage(SendType.Message, senderData.Nick,
                                        string.Format("{0,-20}{1}", x.Created.ToString("g"), x.Command))
                                    );
                            }
                        }
                    }
                    catch
                    {
                    }
                    break;
                case "!DUMP":
                    try
                    {
                        if (user.IsAdmin)
                        {
                            // Dump all url information to XML
                            //BotConfiguration.Current.SaveURLs();

                            //BotUser.BroadcastToAdmins(irc, "Dumped {0} URLs",
                            //    BotConfiguration.Current.MatchedURLs.Count());
                        }
                    }
                    catch
                    {
                    }
                    break;
                case "!SETUP":
                    // Should only be usable if no other admins are listed in the system

                    if (BotConfiguration.Current.Settings.GetAdmins().Count == 0)
                    {
                        // a temp account will have already been setup for this user
                        // no admins, promote guest account to admin
                        user.IsGuest = false;
                        user.IsAdmin = true;
                        user.Email = segments[1];
                        user.Pass = BotUser.ComputeHash(segments[2]);

                        BotConfiguration.Current.SaveConfig<BotSettings>("config");

                        irc.RfcPrivmsg(senderData.Nick,
                            "Thank you, you've been added to the bot as an admin. !SETUP will no longer allow admins to be added for security reasons.");
                    }
                    break;
                case "!SETPERM":
                    try
                    {
                        if (user.IsAdmin)
                        {
                            var permUser = BotConfiguration.Current.Settings.GetUser(segments[1]);
                            if (permUser == null || permUser.IsGuest)
                            {
                                irc.SendReply(senderData, "Unknown user. Ensure they've registered and are not a guest");
                            }
                            var op = segments[2] == "+";
                            switch (segments[2].ToUpper())
                            {
                                case "MOD":
                                    permUser.IsModerator = op;
                                    break;
                                case "ADM":
                                    permUser.IsAdmin = op;
                                    break;
                            }
                        }
                    }
                    catch
                    {
                    }
                    break;
                case "!RELOAD":
                    try
                    {
                        if (user.IsAdmin)
                        {
                            if (segments.Count() == 1)
                            {
                                BotConfiguration.Current.Settings =
                                    BotConfiguration.Current.LoadConfig<BotSettings>("config");
                            }
                            BotUser.BroadcastToAdmins(irc, "{0} forced a reload.", senderData.Nick);
                        }
                    }
                    catch
                    {
                    }
                    break;
                case "!SAVE":
                    try
                    {
                        if (user.IsAdmin)
                        {
                        }
                    }
                    catch
                    {
                    }
                    break;
                case "!QUIT":
                    try
                    {
                        if (user.IsAdmin)
                        {
                            // Quits the bot
                            BotConfiguration.Current.SaveConfig<BotSettings>("config");
                            BotModulesManager.BotListeners.ForEach(x => x.Stop());

                            if (segments.Count() > 1)
                                irc.RfcQuit(string.Join(" ", segments.Skip(1)));
                            else
                                irc.RfcQuit();
                            irc.Disconnect();
                            Environment.Exit(0);
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