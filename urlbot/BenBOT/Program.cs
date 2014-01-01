using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Linq.Dynamic;
using System.Net.Mail;
using System.Text.RegularExpressions;
using System.Threading;
using Meebey.SmartIrc4net;

namespace BenBOT
{
    internal class Program
    {
        public static IrcClient irc = new IrcClient();
        public static BotConfiguration Config = new BotConfiguration();
        public static List<string> BlockedQueryKeywords = new List<string> {".padright", ".padleft", ".pad"};

        private static void Main(string[] args)
        {
            // Load the configuration File
            Config.LoadConfig();
            Config.LoadURLs();

            irc.SendDelay = 400;
            irc.AutoReconnect = true;
            irc.ActiveChannelSyncing = true;

            irc.OnConnected += irc_OnConnected;
            irc.OnConnectionError += irc_OnConnectionError;
            irc.OnErrorMessage += irc_OnErrorMessage;
            irc.OnQueryMessage += irc_OnQueryMessage;
            irc.OnChannelMessage += irc_OnChannelMessage;
            irc.OnReadLine += irc_OnReadLine;
            irc.OnKick += irc_OnKick;
            irc.OnBan += irc_OnBan;

            try
            {
                irc.Connect("port80a.se.quakenet.org", 6667);
                while (true)
                {
                    irc.Listen(false);
                    Thread.Sleep(10);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                Console.ReadKey();
            }
        }

        
        public static void BroadcastToAdmins(string message, params object[] paramStrings)
        {
            try
            {
                var msg = string.Format(message, paramStrings);
                var admins = Config.Settings.GetAdmins();
                if (admins != null && admins.Count > 0)
                {
                    admins.ForEach(x => irc.RfcPrivmsg(x.Nick, msg));
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        public static bool IsQueryValid(string query)
        {
            return !BlockedQueryKeywords.Any(query.ToLower().Contains);
        }

        public static void ParseMessage(IrcEventArgs e)
        {
            if (e.Data.Message.StartsWith("!"))
            {
                var user = Config.Settings.GetUser(e.Data.Nick);
                if (user == null)
                {
                    // Add guest account for command tracking
                    user = new BotUser()
                    {
                        Nick = e.Data.Nick,
                        IsGuest = true
                    };
                    Config.Settings.KnownUsers.Add(user);
                }

                // If guest, enforce command limit
                if(user.IsGuest)
                    if (user.CommandsInLast(60) == 2)
                    {
                        irc.SendMessage(SendType.Message, e.Data.Nick, "Maximum commands per minute for guest reached.");
                        user.CommandHistory.Add(new HistoryItem()
                        {
                            Command = e.Data.RawMessage,
                            Created = DateTime.Now
                        });
                        return;
                    }else if (user.CommandsInLast(60) > 2)
                        return;

                // Command
                var segments = e.Data.Message.Split(new[] {' '});
                switch (segments[0].ToUpper())
                {
                    case "!ADDMATCH":
                        if (user.IsAdmin)
                        {
                            try
                            {
                                Config.Settings.MatchActions.Add(new ActionMatch
                                {
                                    Action = segments[1],
                                    MatchString = segments[2],
                                    Reason = string.Join(" ", segments.Skip(2))
                                });
                                Config.SaveConfig();
                            }
                            catch
                            {
                            }
                        }
                        break;
                    case "!URLS":
                        List<MatchedURL> rtnUrls;

                        rtnUrls = FilterURLs(segments).OrderByDescending(x => x.DateTime).Take(10).ToList();

                        if (rtnUrls.Any())
                        {
                            irc.SendMessage(SendType.Message, e.Data.Nick,
                                string.Format("{0,-20}{1,-15}{2,-10}{3}", "Channel", "Nick", "Time", "URL"));
                            foreach (var url in rtnUrls)
                            {
                                irc.SendMessage(SendType.Message, e.Data.Nick,
                                    string.Format("{0,-20}{1,-15}{2,-10}{3}", url.Channel, url.Nick,
                                        url.DateTime.ToString("g"), url.URL));
                            }
                        }
                        else
                        {
                            irc.SendMessage(SendType.Message, e.Data.Nick, "No Matches Found");
                        }
                        break;
                    case "!QUERY":
                        var rtn = Config.MatchedURLs;
                        try
                        {
                            string query = string.Empty;

                            switch (segments[1].ToUpper())
                            {
                                case "SAVE":
                                    if (user != null)
                                    {
                                        query = string.Join(" ", segments.Skip(3));
                                        if (!IsQueryValid(query))
                                            return;
                                        user.SavedQueries.Add(new SavedQuery
                                        {
                                            Name = segments[2],
                                            Query = query
                                        });

                                        Config.SaveConfig();
                                        irc.SendMessage(SendType.Message, e.Data.Nick, "Saved new query: " + segments[2]);
                                    }
                                    break;
                                case "RUN":
                                    if (user != null)
                                    {
                                        var savedquery =
                                            user.SavedQueries.SingleOrDefault(x => x.Name == segments[2]);
                                        if (savedquery != null)
                                            query = savedquery.Query;
                                        else
                                        {
                                            irc.SendMessage(SendType.Message, e.Data.Nick, "Query not found");
                                            return;
                                        }
                                    }
                                    break;
                                default:
                                    query = string.Join(" ", segments.Skip(1));
                                    if (!IsQueryValid(query))
                                        return;
                                    break;
                            }

                            rtn = rtn.AsQueryable().Where(query).Take(10).ToList();
                            if (rtn.Any())
                            {
                                irc.SendMessage(SendType.Message, e.Data.Nick,
                                    string.Format("{0,-20}{1,-15}{2,-10}{3}", "Channel", "Nick", "Time", "URL"));
                                foreach (var url in rtn)
                                {
                                    irc.SendMessage(SendType.Message, e.Data.Nick,
                                        string.Format("{0,-20}{1,-15}{2,-10}{3}", url.Channel, url.Nick,
                                            url.DateTime.ToString("g"), url.URL));
                                }
                            }
                            else
                            {
                                irc.SendMessage(SendType.Message, e.Data.Nick, "No Matches Found");
                            }
                        }
                        catch (Exception ex)
                        {
                            irc.SendMessage(SendType.Message, e.Data.Nick, ex.Message);
                        }
                        break;
                    case "!JOIN":
                        // Save new channel to join list
                        try
                        {
                            if (user.IsAdmin)
                            {
                                irc.RfcJoin(segments[1]);
                                Config.Settings.AutoJoinChannels.Add(segments[1]);

                                // Save Configuration
                                Config.SaveConfig();

                                BroadcastToAdmins("Joined channel {0} by {1}", segments[1], e.Data.Nick);
                            }
                            else
                            {
                                BroadcastToAdmins(
                                    "{0} attempted to force a channel join for channel {1} and is not an admin",
                                    e.Data.Nick, segments.Any() ? segments[1] : "<empty channel name>");
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
                                var partChan = string.Empty;
                                partChan = segments.Count() > 1 ? segments[1] : e.Data.Channel;
                                irc.RfcPart(partChan);
                                Config.Settings.AutoJoinChannels.Remove(partChan);

                                BroadcastToAdmins("Parted channel {0} by {1}", segments[1], e.Data.Nick);
                            }
                            else
                            {
                                BroadcastToAdmins(
                                    "{0} attempted to force a channel part for channel {1} and is not an admin",
                                    e.Data.Nick, segments.Any() ? segments[1] : "<empty channel name>");
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
                                Config.SaveConfig();
                            }
                        }
                        catch
                        {
                        }
                        break;
                    case "!URLSTATS":
                        var stats = from x in FilterURLs(segments)
                            group x by x.Channel
                            into xG
                            select new
                            {
                                Channel = xG.Key,
                                URLCount = xG.Count()
                            };
                        irc.SendMessage(SendType.Message, e.Data.Nick,
                            "Current URL Cache Size: " + Config.MatchedURLs.Count());

                        irc.SendMessage(SendType.Message, e.Data.Nick,
                            string.Format("{0,-20}{1,-10}", "Channel", "URL Count"));
                        foreach (var item in stats)
                        {
                            irc.SendMessage(SendType.Message, e.Data.Nick,
                                string.Format("{0,-20}{1,-10}", item.Channel, item.URLCount));
                        }

                        break;
                    case "!REGISTER":
                        try
                        {
                            var newUser = Config.Settings.KnownUsers.SingleOrDefault(x => x.Nick == e.Data.Nick);
                            if (newUser == null)
                            {
                                newUser = new BotUser
                                {
                                    DefaultHost = e.Data.Host,
                                    Email = segments[1],
                                    IsAdmin = false,
                                    Nick = e.Data.Nick,
                                    Pass = segments[2]
                                };
                            }
                            else if(newUser.IsGuest)
                            {
                                newUser.DefaultHost = e.Data.Host;
                                newUser.Email = segments[1];
                                newUser.IsAdmin = false;
                                newUser.Nick = e.Data.Nick;
                                newUser.Pass = segments[2];
                                newUser.IsGuest = false;
                            }
                            else
                            {
                                // user already registered
                            }

                            irc.SendMessage(SendType.Message, e.Data.Nick,
                                string.Format(
                                    "Welcome {0}, we've registered you with the following email address {1}.",
                                    e.Data.Nick, segments[1]));

                            Config.SaveConfig();
                        }
                        catch
                        {
                        }
                        break;
                    case "!EMAIL":
                        try
                        {
                            if (user != null)
                            {
                                var body = string.Empty;
                                var urls = FilterURLs(segments);
                                body = urls.Aggregate(body, (current, item) => current + string.Format("{0,-10} {1,-6} {2}\n", 
                                    item.Channel, item.DateTime.ToShortTimeString(), item.URL));

                                var mm = new MailMessage(Config.Settings.SMTPSettings.DefaultEmailAddress, user.Email,
                                    "URL List", body);

                                var smtp = new SmtpClient(Config.Settings.SMTPSettings.SMTPHost);
                                smtp.Send(mm);
                            }
                            else
                            {
                                irc.SendMessage(SendType.Message, e.Data.Nick,
                                    "Not Registered! Use !register <emailaddress> <password>");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                        }
                        break;
                    case "!CHANS":
                        try
                        {
                            if (user != null && user.IsAdmin)
                            {
                                irc.SendMessage(SendType.Message, e.Data.Nick, "Active Channel List:");
                                foreach (var chan in irc.GetChannels())
                                {
                                    irc.SendMessage(SendType.Message, e.Data.Nick, chan);
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
                            if (user != null && user.IsAdmin)
                            {
                                List<HistoryItem> history = new List<HistoryItem>();
                                if (segments.Count() > 1)
                                {
                                    var histNick = segments[1];
                                    var botUser = Config.Settings.KnownUsers.SingleOrDefault(x => x.Nick == histNick);
                                    if (botUser != null)
                                        history = botUser.CommandHistory.Take(10).ToList();
                                }
                                else
                                    history = Config.Settings.KnownUsers.SelectMany(x => x.CommandHistory).Take(10).ToList();

                                if (history != null)
                                {
                                    irc.SendMessage(SendType.Message, e.Data.Nick, "Command History List (last 10):");
                                    history.ToList().ForEach(x =>
                                        irc.SendMessage(SendType.Message, e.Data.Nick, string.Format("{0,-10}{1}", x.Created.ToString("g"), x.Command))
                                    );
                                }else
                                    irc.SendMessage(SendType.Message, e.Data.Nick, "No history to display");
                            }
                        }
                        catch
                        {
                        }
                        break;
                    case "!DUMP":
                        try
                        {
                            if (user != null && user.IsAdmin)
                            {
                                // Dump all url information to XML
                                Config.SaveURLs();

                                BroadcastToAdmins("Dumped {0} URLs", Config.MatchedURLs.Count());
                            }
                        }
                        catch
                        {
                        }
                        break;
                    case "!SETUP":
                        // Should only be usable if no other admins are listed in the system
                        if (Config.Settings.GetAdmins().Count == 0)
                        {
                            // no admins
                            Config.Settings.KnownUsers.Add(new BotUser
                            {
                                DefaultHost = e.Data.Host,
                                Email = segments[1],
                                IsAdmin = true,
                                Nick = e.Data.Nick,
                                Pass = segments[2]
                            });
                            Config.SaveConfig();

                            irc.RfcPrivmsg(e.Data.Nick,
                                "Thank you, you've been added to the bot as an admin. !SETUP will no longer allow admins to be added for security reasons.");
                        }
                        break;
                    case "!QUIT":
                        try
                        {
                            if (user != null && user.IsAdmin)
                            {
                                // Quits the bot
                                Config.SaveURLs();
                                if(segments.Count() > 1)
                                    irc.RfcQuit(string.Join(" ", segments.Take(1)));
                                irc.Disconnect();
                                Environment.Exit(0);
                            }
                        }
                        catch
                        {
                        }

                        break;
                    case "!HELP":
                        irc.SendMessage(SendType.Message, e.Data.Nick, "urlbot Help");
                        irc.SendMessage(SendType.Message, e.Data.Nick,
                            string.Format("{0,-20}{1}", "!urls", "Returns a list of URLs that have been captured"));
                        irc.SendMessage(SendType.Message, e.Data.Nick,
                            string.Format("{0,-20}{1}", "-- matching", "Single keyword search. E.g. \"matching foobar\""));
                        irc.SendMessage(SendType.Message, e.Data.Nick,
                            string.Format("{0,-20}{1}", "-- last",
                                "Denotes a time period. last [interval] [interval type]"));
                        break;
                }

                // Add command to history tracker
                user.CommandHistory.Add(new HistoryItem()
                {
                    Command = e.Data.Message,
                    Created = DateTime.Now
                });
            }
            else
            {
                // To be parsed for data
                var regx =
                    new Regex(
                        "(http|https)://([\\w+?\\.\\w+])+([a-zA-Z0-9\\~\\!\\@\\#\\$\\%\\^\\&amp;\\*\\(\\)_\\-\\=\\+\\\\\\/\\?\\.\\:\\;\\'\\,]*)?",
                        RegexOptions.IgnoreCase);
                var mactches = regx.Matches(e.Data.Message);
                foreach (Match match in mactches)
                {
                    Console.WriteLine("Found URL: " + match.Value);
                    Config.MatchedURLs.Add(new MatchedURL
                    {
                        Channel = e.Data.Channel,
                        DateTime = DateTime.Now,
                        Nick = e.Data.Nick,
                        URL = match.Value
                    });
                    var action = Config.Settings.CheckActions(match.Value);
                    
                    if (action == null) continue;
                    var user = Config.Settings.GetUser(e.Data.Nick);
                    if (user != null)
                        if (user.IsAdmin)
                            break;
                    if (irc.GetChannelUser(e.Data.Channel, e.Data.Nick).IsOp)
                        break;

                    switch (action.Action.ToUpper())
                    {
                        case "KICK":
                            irc.RfcKick(e.Data.Channel, e.Data.Nick, action.Reason);
                            break;
                    }
                }
            }
        }

        private static IEnumerable<MatchedURL> FilterURLs(string[] segments)
        {
            var rtnUrls = Config.MatchedURLs;
            try
            {
                // Loop from the next segment on
                for (int i = 1; i < segments.Count(); i++)
                {
                    string seg = segments[i];
                    if (seg.StartsWith("#"))
                        rtnUrls = rtnUrls.Where(x => x.Channel.ToUpper() == seg.ToUpper()).ToList();
                    else if (seg.StartsWith("@"))
                        rtnUrls = rtnUrls.Where(x => x.Nick.ToUpper() == seg.Replace("@", "").ToUpper()).ToList();
                    else
                    {
                        switch (seg.ToUpper())
                        {
                            case "TODAY":
                                rtnUrls = rtnUrls.Where(x => x.DateTime.Date == DateTime.Now.Date).ToList();
                                break;
                            case "YESTERDAY":
                                rtnUrls = rtnUrls.Where(x => x.DateTime.Date == DateTime.Now.Date.AddDays(-1)).ToList();
                                break;
                            case "MATCHING":
                                string keyword = segments[++i];
                                rtnUrls = rtnUrls.Where(x => x.URL.Contains(keyword)).ToList();
                                break;
                            case "LAST":
                                try
                                {
                                    var interval = int.Parse(segments[++i]);
                                    var type = segments[++i];
                                    var searchPeriod = 0;
                                    switch (type)
                                    {
                                        case "days":
                                        case "day":
                                            searchPeriod = interval*60*60*24;
                                            break;
                                        case "hours":
                                        case "hour":
                                            searchPeriod = interval*60*60;
                                            break;
                                        case "minutes":
                                        case "min":
                                        case "mins":
                                            searchPeriod = interval*60;
                                            break;
                                        case "seconds":
                                        case "sec":
                                        case "secs":
                                            searchPeriod = interval;
                                            break;
                                    }
                                    rtnUrls =
                                        rtnUrls.Where(x => x.DateTime > DateTime.Now.AddSeconds(-searchPeriod)).ToList();
                                }
                                catch
                                {
                                }
                                break;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }

            return rtnUrls;
        }

        #region IRC Events

        private static void irc_OnKick(object sender, KickEventArgs e)
        {
            BroadcastToAdmins("Kicked from channel {0} by {1}: {2}", e.Channel, e.Whom, e.KickReason);
            // Auto rejoin channel
            irc.RfcJoin(e.Channel);
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
            irc.Login(Config.Settings.BotName, "Stupid Bot");

            foreach (string chan in Config.Settings.AutoJoinChannels)
            {
                irc.RfcJoin(chan);
            }
        }

        static void irc_OnBan(object sender, BanEventArgs e)
        {
            // need a way to compare mask to current ident?
            if (e.Hostmask.Contains(irc.Nickname))
            {
                BroadcastToAdmins("banned from channel {0} by {1}: {2}", e.Channel, e.Who, e.Hostmask);
                irc.Unban(e.Channel, e.Hostmask);
            }
        }

        #endregion
    }
}