using System;
using System.Collections.Generic;
using System.Linq;
using BenBOT.Configuration;
using BenBOT.Models;
using Meebey.SmartIrc4net;

namespace BenBOT.BotCommands
{
    public class URLsAction : IBotCommand
    {
        public string[] CommandsHandled
        {
            get { return new[] {"!URLS"}; }
        }

        public string[] HelpMessage(string command)
        {
            return new[] {""};
        }

        public void ProcessCommand(string[] segments, BotUser user, IrcClient irc, IrcMessageData senderData)
        {
            List<MatchedURL> rtnUrls = FilterURLs(segments, user).OrderByDescending(x => x.DateTime).Take(10).ToList();
            try
            {
                if (rtnUrls.Any())
                {
                    irc.SendMessage(SendType.Message, senderData.Nick,
                        string.Format("{0,-20}{1,-20}{2,-10}{3}", "Channel", "Nick", "Time", "URL"));
                    foreach (var url in rtnUrls)
                    {
                        irc.SendMessage(SendType.Message, senderData.Nick,
                            string.Format("{0,-20}{1,-20}{2,-10}{3}", url.Channel, url.Nick,
                                url.DateTime.ToString("g"), url.URL));
                    }
                }
                else
                {
                    irc.SendMessage(SendType.Message, senderData.Nick, "No Matches Found");
                }
            }
            catch
            {
            }
        }

        private IEnumerable<MatchedURL> FilterURLs(string[] segments, BotUser user = null)
        {
            var rtnUrls = BotConfiguration.Current.Config<List<MatchedURL>>("MatchedUrls");
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
                            case "LASTSPOKE":
                                if (user != null)
                                    rtnUrls = rtnUrls.Where(x => x.DateTime >= user.LastSpoke).ToList();
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
    }
}