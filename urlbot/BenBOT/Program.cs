using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using BenBOT.Configuration;
using Meebey.SmartIrc4net;

namespace BenBOT
{
    internal class Program
    {
        public static IrcClient Irc = new IrcClient();

        public static List<IBotCommand> BotCommands = new List<IBotCommand>();
        public static List<IBotListener> BotListeners = new List<IBotListener>();

        private static void Main(string[] args)
        {
            Irc.SendDelay = 400;
            Irc.AutoReconnect = true;
            Irc.ActiveChannelSyncing = true;

            Irc.OnConnected += irc_OnConnected;
            Irc.OnConnectionError += irc_OnConnectionError;
            Irc.OnErrorMessage += irc_OnErrorMessage;
            Irc.OnQueryMessage += irc_OnQueryMessage;
            Irc.OnChannelMessage += irc_OnChannelMessage;
            Irc.OnReadLine += irc_OnReadLine;
            Irc.OnKick += irc_OnKick;
            Irc.OnBan += irc_OnBan;

            LoadBotCommands();
            

            try
            {
                Irc.Connect("port80a.se.quakenet.org", 6667);
                
                LoadBotListeners();

                while (true)
                {
                    Irc.Listen(false);
                    Thread.Sleep(10);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                Console.ReadKey();
            }
        }

        public static void LoadBotCommands()
        {
            Type[] botCommands =
                Assembly.GetCallingAssembly()
                    .GetTypes()
                    .Where(t => String.Equals(t.Namespace, "BenBOT.BotCommands", StringComparison.Ordinal))
                    .ToArray();
            foreach (Type cmd in botCommands)
            {
                if (cmd.GetInterfaces().Contains(typeof(IBotCommand)))
                    BotCommands.Add((IBotCommand) Assembly.GetCallingAssembly().CreateInstance(cmd.FullName));
            }
        }

        public static void LoadBotListeners()
        {
            Type[] botCommands =
                Assembly.GetCallingAssembly()
                    .GetTypes()
                    .Where(t => String.Equals(t.Namespace, "BenBOT.BotListeners", StringComparison.Ordinal))
                    .ToArray();
            foreach (Type cmd in botCommands)
            {
                if (cmd.GetInterfaces().Contains(typeof (IBotListener)))
                {
                    var listener = (IBotListener) Assembly.GetCallingAssembly().CreateInstance(cmd.FullName);

                    if (listener != null)
                    {
                        listener.Init(Irc);
                        listener.Start();
                        BotListeners.Add(listener);
                    }
                }
            }
        }

        public static IBotCommand GetBotCommand(string command)
        {
            return BotCommands.SingleOrDefault(x => x.CommandsHandled.Contains(command.ToUpper()));
        }

        public static void ParseMessage(IrcEventArgs e)
        {
            BotUser user = BotConfiguration.Current.Settings.GetUser(e.Data.Nick);
            if (user == null)
            {
                // Add guest account for command tracking
                user = new BotUser
                {
                    Nick = e.Data.Nick,
                    IsGuest = true
                };
                BotConfiguration.Current.Settings.KnownUsers.Add(user);
            }


            if (e.Data.Message.StartsWith("!"))
            {
                // Command
                // If guest, enforce command limit
                if (user.IsGuest)
                    if (user.CommandsInLast(60) == 2)
                    {
                        Irc.SendMessage(SendType.Message, e.Data.Nick, "Maximum commands per minute for guest reached.");
                        user.CommandHistory.Add(new HistoryItem
                        {
                            Command = e.Data.RawMessage,
                            Created = DateTime.Now
                        });
                        return;
                    }
                    else if (user.CommandsInLast(60) > 2)
                        return;


                string[] segments = e.Data.Message.Split(new[] {' '});
                IBotCommand command = GetBotCommand(segments[0].ToUpper());
                if (command == null)
                    return;

                try
                {
                    command.ProcessCommand(segments, user, Irc, e.Data);
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }

                // Add command to history tracker
                user.CommandHistory.Add(new HistoryItem
                {
                    Command = e.Data.Message,
                    Created = DateTime.Now
                });


                // Log last spoke
                user.LastSpoke = DateTime.Now;
            }
        }

        #region IRC Events

        private static void irc_OnKick(object sender, KickEventArgs e)
        {
            BotUser.BroadcastToAdmins((IrcClient) sender, "Kicked from channel {0} by {1}: {2}", e.Channel, e.Whom,
                e.KickReason);
            // Auto rejoin channel
            Irc.RfcJoin(e.Channel);
        }

        private static void irc_OnReadLine(object sender, ReadLineEventArgs e)
        {
            Console.WriteLine("-- " + e.Line);
        }

        private static void irc_OnErrorMessage(object sender, IrcEventArgs e)
        {
            Console.WriteLine(e.Data.Message);
        }

        private static void irc_OnConnectionError(object sender, EventArgs e)
        {
            Console.WriteLine("COnnection Error");
        }

        private static void irc_OnChannelMessage(object sender, IrcEventArgs e)
        {
            ParseMessage(e);
        }

        private static void irc_OnQueryMessage(object sender, IrcEventArgs e)
        {
            ParseMessage(e);
        }

        private static void irc_OnConnected(object sender, EventArgs e)
        {
            Irc.Login(BotConfiguration.Current.Settings.BotName, "Stupid Bot");

            foreach (string chan in BotConfiguration.Current.Settings.AutoJoinChannels)
            {
                Irc.RfcJoin(chan);
            }
        }

        private static void irc_OnBan(object sender, BanEventArgs e)
        {
            // need a way to compare mask to current ident?
            if (e.Hostmask.Contains(Irc.Nickname))
            {
                BotUser.BroadcastToAdmins((IrcClient) sender, "banned from channel {0} by {1}: {2}", e.Channel, e.Who,
                    e.Hostmask);
                Irc.Unban(e.Channel, e.Hostmask);
            }
        }

        #endregion
    }
}