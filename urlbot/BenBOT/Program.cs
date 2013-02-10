using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Mail;

using Meebey.SmartIrc4net;
using System.Text.RegularExpressions;

using System.Linq.Dynamic;


namespace BenBOT
{
    class Program
    {

        public static IrcClient irc = new IrcClient();
        public static BotConfiguration Config = new BotConfiguration();

        static void Main(string[] args)
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


            try
            {
                irc.Connect("port80a.se.quakenet.org", 6667);
                while (true)
                {
                    irc.Listen(false);
                    System.Threading.Thread.Sleep(10);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                Console.ReadKey();
            }

        }

        static public void ParseMessage(IrcEventArgs e)
        {
            if (e.Data.Message.StartsWith("!"))
            {
                var user = Config.Settings.GetUser(e.Data.Nick);

                // Command
                string[] segments = e.Data.Message.Split(new char[] { ' ' });
                switch (segments[0].ToUpper())
                {
                    case "!URLS":
                        List<MatchedURL> rtnUrls = Config.MatchedURLs;

                        rtnUrls = FilterURLs(segments).OrderByDescending(x => x.DateTime).Take(10).ToList();

                        if (rtnUrls.Count() > 0)
                        {
                            irc.SendMessage(SendType.Message, e.Data.Nick, string.Format("{0,-20}{1,-15}{2,-10}{3}", "Channel", "Nick", "Time", "URL"));
                            foreach (var url in rtnUrls)
                            {
                                irc.SendMessage(SendType.Message, e.Data.Nick, string.Format("{0,-20}{1,-15}{2,-10}{3}", url.Channel, url.Nick, url.DateTime.ToShortTimeString(), url.URL));
                            }
                        }
                        else
                        {
                            irc.SendMessage(SendType.Message, e.Data.Nick, "No Matches Found");
                        }
                        break;
                    case "!QUERY":
                        List<MatchedURL> rtn = Config.MatchedURLs;
                        try
                        {
                            string query = string.Empty;
                            
                            switch (segments[1].ToUpper())
                            {
                                case "SAVE":
                                    if (user != null)
                                    {
                                        query = string.Join(" ", segments.Skip(3));
                                        user.SavedQueries.Add(new SavedQuery()
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
                                        var savedquery = user.SavedQueries.Where(x => x.Name == segments[2]).SingleOrDefault();
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
                                    break;
                            }

                            rtn = rtn.AsQueryable().Where(query).ToList();
                            if (rtn.Count() > 0)
                            {
                                irc.SendMessage(SendType.Message, e.Data.Nick, string.Format("{0,-20}{1,-15}{2,-10}{3}", "Channel", "Nick", "Time", "URL"));
                                foreach (var url in rtn)
                                {
                                    irc.SendMessage(SendType.Message, e.Data.Nick, string.Format("{0,-20}{1,-15}{2,-10}{3}", url.Channel, url.Nick, url.DateTime.ToShortTimeString(), url.URL));
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
                            }
                        }
                        catch { }
                        break;
                    case "!PART":
                        try
                        {
                            if (user.IsAdmin)
                            {
                                string partChan = string.Empty;
                                if (segments.Count() > 1)
                                    partChan = segments[1];
                                else
                                    partChan = e.Data.Channel;
                                irc.RfcPart(partChan);
                                Config.Settings.AutoJoinChannels.Remove(partChan);
                            }
                        }
                        catch { }
                        break;
                    case "!SAVE":
                        try
                        {
                            if (user.IsAdmin)
                            {
                                Config.SaveConfig();
                            }
                        }
                        catch { }
                        break;
                    case "!URLSTATS":
                        var stats = from x in FilterURLs(segments)
                                    group x by x.Channel into xG
                                    select new
                                    {
                                        Channel = xG.Key,
                                        URLCount = xG.Count()
                                    };
                        irc.SendMessage(SendType.Message, e.Data.Nick, "Current URL Cache Size: " + Config.MatchedURLs.Count().ToString());

                        irc.SendMessage(SendType.Message, e.Data.Nick, string.Format("{0,-20}{1,-10}", "Channel", "URL Count"));
                        foreach (var item in stats)
                        {
                            irc.SendMessage(SendType.Message, e.Data.Nick, string.Format("{0,-20}{1,-10}", item.Channel, item.URLCount.ToString()));
                        }

                        break;
                    case "!REGISTER":
                        try
                        {
                            Config.Settings.KnownUsers.Add(new BotUser()
                            {
                                DefaultHost = e.Data.Host,
                                Email = segments[1],
                                IsAdmin = false,
                                Nick = e.Data.Nick,
                                Pass = segments[2]
                            });

                            irc.SendMessage(SendType.Message, e.Data.Nick, string.Format("Welcome {0}, we've registered you with the following email address {1}.", e.Data.Nick, segments[1]));

                            Config.SaveConfig();
                        }
                        catch { }
                        break;
                    case "!EMAIL":
                        try
                        {
                            if (user != null)
                            {
                                string body = string.Empty;
                                var urls = FilterURLs(segments);
                                foreach (var item in urls)
                                {
                                    body += string.Format("{0,-10} {1,-6} {2}\n", item.Channel, item.DateTime.ToShortTimeString(), item.URL);
                                }
                                MailMessage mm = new MailMessage(Config.Settings.SMTPSettings.DefaultEmailAddress, user.Email, "URL List", body);

                                SmtpClient smtp = new SmtpClient(Config.Settings.SMTPSettings.SMTPHost);
                                smtp.Send(mm);
                            }
                            else
                            {
                                irc.SendMessage(SendType.Message, e.Data.Nick, "Not Registered! Use !register <emailaddress> <password>");
                            }
                        }
                        catch(Exception ex) {
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
                        catch { }
                        break;
                    case "!DUMP":
                        try
                        {
                            if (user!=null && user.IsAdmin)
                            {
                                // Dump all url information to XML
                                Config.SaveURLs();
                                
                                
                                string response = string.Format("Dumped {0} URLs", Config.MatchedURLs.Count());
                                Console.WriteLine(response);
                                irc.SendMessage(SendType.Message, e.Data.Nick, response);
                            }
                        }
                        catch { }
                        break;

                    case "!QUIT":

                        try
                        {
                            if (user != null && user.IsAdmin)
                            {
                                // Quits the bot
                                Config.SaveURLs();
                                irc.Disconnect();
                                System.Environment.Exit(0);
                            }
                        }
                        catch { }

                        break;
                    case "!HELP":
                        irc.SendMessage(SendType.Message, e.Data.Nick, "urlbot Help");
                        irc.SendMessage(SendType.Message, e.Data.Nick, string.Format("{0,-20}{1}", "!urls", "Returns a list of URLs that have been captured"));
                        irc.SendMessage(SendType.Message, e.Data.Nick, string.Format("{0,-20}{1}", "-- matching", "Single keyword search. E.g. \"matching foobar\""));
                        irc.SendMessage(SendType.Message, e.Data.Nick, string.Format("{0,-20}{1}", "-- last", "Denotes a time period. last [nterval] [interval type]"));
                        break;
                }
            }
            else
            {
                // To be parsed for data
                Regex regx = new Regex("(http|https)://([\\w+?\\.\\w+])+([a-zA-Z0-9\\~\\!\\@\\#\\$\\%\\^\\&amp;\\*\\(\\)_\\-\\=\\+\\\\\\/\\?\\.\\:\\;\\'\\,]*)?", RegexOptions.IgnoreCase);        
                MatchCollection mactches = regx.Matches(e.Data.Message);
                foreach (Match match in mactches)
                {
                   
                    Console.WriteLine("Found URL: " + match.Value);
                    Config.MatchedURLs.Add(new MatchedURL()
                    {
                        Channel = e.Data.Channel,
                        DateTime = DateTime.Now,
                        Nick = e.Data.Nick,
                        URL = match.Value
                    });
                    
                }
            }
        }

        private static List<MatchedURL> FilterURLs(string[] segments)
        {
            List<MatchedURL> rtnUrls = Config.MatchedURLs;
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
                                    int interval = int.Parse(segments[++i]);
                                    string type = segments[++i];
                                    int searchPeriod = 0;
                                    switch (type)
                                    {
                                        case "days":
                                        case "day": searchPeriod = interval * 60 * 60 * 24; break;
                                        case "hours":
                                        case "hour": searchPeriod = interval * 60 * 60; break;
                                        case "minutes":
                                        case "min":
                                        case "mins": searchPeriod = interval * 60; break;
                                        case "seconds":
                                        case "sec":
                                        case "secs": searchPeriod = interval; break;
                                    }
                                    rtnUrls = rtnUrls.Where(x => x.DateTime > DateTime.Now.AddSeconds(-searchPeriod)).ToList();
                                }
                                catch { }
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
        static void irc_OnKick(object sender, KickEventArgs e)
        {
            // Auto rejoin channel
            irc.RfcJoin(e.Channel);
        }

        static void irc_OnReadLine(object sender, ReadLineEventArgs e)
        {
            Console.WriteLine("-- " + e.Line);
        }

        static void irc_OnErrorMessage(object sender, IrcEventArgs e)
        {
            Console.WriteLine(e.Data.Message);
        }

        static void irc_OnConnectionError(object sender, EventArgs e)
        {
            Console.WriteLine("COnnection Error");
        }

        static void irc_OnChannelMessage(object sender, IrcEventArgs e)
        {
            ParseMessage(e);
        }

        static void irc_OnQueryMessage(object sender, IrcEventArgs e)
        {
            ParseMessage(e);
        }

        static void irc_OnConnected(object sender, EventArgs e)
        {
            irc.Login(Config.Settings.BotName, "Stupid Bot");

            foreach (var chan in Config.Settings.AutoJoinChannels)
            {
                irc.RfcJoin(chan);
            }
            
        } 
        #endregion
    }
}
