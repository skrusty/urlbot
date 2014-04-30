﻿using System;
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

        public string[] HelpMessage(string command)
        {
            return new[] {""};
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
                            foreach (string chan in irc.GetChannels())
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
                                string histNick = segments[1];
                                BotUser botUser =
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
                            BotConfiguration.Current.SaveURLs();

                            BotUser.BroadcastToAdmins(irc, "Dumped {0} URLs",
                                BotConfiguration.Current.MatchedURLs.Count());
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
                        user.Pass = segments[2];

                        BotConfiguration.Current.SaveConfig();

                        irc.RfcPrivmsg(senderData.Nick,
                            "Thank you, you've been added to the bot as an admin. !SETUP will no longer allow admins to be added for security reasons.");
                    }
                    break;
                case "!SETPERM":
                    try
                    {
                        if (user.IsAdmin)
                        {
                            BotUser permUser = BotConfiguration.Current.Settings.GetUser(segments[1]);
                            if (permUser == null || permUser.IsGuest)
                            {
                                irc.SendReply(senderData, "Unknown user. Ensure they've registered and are not a guest");
                            }
                            bool op = segments[2] == "+";
                            switch (segments[2].ToUpper())
                            {
                                case "MOD":

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
                                BotConfiguration.Current.LoadConfig();
                                BotConfiguration.Current.LoadURLs();
                            }
                            else
                            {
                                switch (segments[1].ToUpper())
                                {
                                    case "CONFIG":
                                        BotConfiguration.Current.LoadConfig();
                                        break;
                                    case "URLS":
                                        BotConfiguration.Current.LoadURLs();
                                        break;
                                }
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
                            BotConfiguration.Current.SaveConfig();
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
                            BotConfiguration.Current.SaveURLs();
                            BotConfiguration.Current.SaveConfig();

                            if (segments.Count() > 1)
                                irc.RfcQuit(string.Join(" ", segments.Take(1)));
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