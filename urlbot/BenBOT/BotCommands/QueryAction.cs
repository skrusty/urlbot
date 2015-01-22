using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Dynamic;
using BenBOT.Configuration;
using BenBOT.Models;
using Meebey.SmartIrc4net;

namespace BenBOT.BotCommands
{
    public class QueryAction : IBotCommand
    {
        private readonly List<string> _blockedQueryKeywords = new List<string> {".padright", ".padleft", ".pad"};

        public string[] CommandsHandled
        {
            get { return new[] {"!QUERY"}; }
        }

        public string[] HelpMessage(string command)
        {
            return new[] {""};
        }

        public void ProcessCommand(string[] segments, BotUser user, IrcClient irc, IrcMessageData senderData)
        {
            var rtn = BotConfiguration.Current.Config<List<MatchedURL>>("MatchedUrls");
            try
            {
                var query = string.Empty;

                switch (segments[1].ToUpper())
                {
                    case "SAVE":
                        query = string.Join(" ", segments.Skip(3));
                        if (!IsQueryValid(query))
                            return;
                        user.SavedQueries.Add(new SavedQuery
                        {
                            Name = segments[2],
                            Query = query
                        });

                        BotConfiguration.Current.SaveConfig<BotSettings>("config");
                        irc.SendMessage(SendType.Message, senderData.Nick, "Saved new query: " + segments[2]);
                        break;
                    case "RUN":
                    {
                        var savedquery =
                            user.SavedQueries.SingleOrDefault(x => x.Name == segments[2]);
                        if (savedquery != null)
                            query = savedquery.Query;
                        else
                        {
                            irc.SendMessage(SendType.Message, senderData.Nick, "Query not found");
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
                    irc.SendMessage(SendType.Message, senderData.Nick,
                        string.Format("{0,-20}{1,-20}{2,-10}{3}", "Channel", "Nick", "Time", "URL"));
                    foreach (var url in rtn)
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
            catch (Exception ex)
            {
                irc.SendMessage(SendType.Message, senderData.Nick, ex.Message);
            }
        }

        private bool IsQueryValid(string query)
        {
            return !_blockedQueryKeywords.Any(query.ToLower().Contains);
        }
    }
}