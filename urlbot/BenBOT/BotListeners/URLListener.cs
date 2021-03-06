﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;
using BenBOT.Configuration;
using BenBOT.Models;
using Meebey.SmartIrc4net;

namespace BenBOT.BotListeners
{
    public class UrlListener : IBotListener
    {
        private IrcClient _irc;

        public void Init(IrcClient irc)
        {
            _irc = irc;
            BotConfiguration.Current.RegisterConfig<List<MatchedURL>>("MatchedUrls", new List<MatchedURL>());
        }

        public void Start()
        {
            _irc.OnChannelMessage += _irc_OnChannelMessage;
            _irc.OnQueryMessage += _irc_OnQueryMessage;
        }

        public void Stop()
        {
            BotConfiguration.Current.SaveConfig<List<MatchedURL>>("MatchedUrls");
        }

        private void _irc_OnQueryMessage(object sender, IrcEventArgs e)
        {
            if (!e.Data.Message.StartsWith("!"))
                ProcessMessage(e);
        }

        private void _irc_OnChannelMessage(object sender, IrcEventArgs e)
        {
            if (!e.Data.Message.StartsWith("!"))
                ProcessMessage(e);
        }

        private void ProcessMessage(IrcEventArgs e)
        {
            var user = BotConfiguration.Current.Settings.GetUser(e.Data.Nick);

            // To be parsed for data
            var regx =
                new Regex(
                    "(http|https)://([\\w+?\\.\\w+])+([a-zA-Z0-9\\~\\!\\@\\#\\$\\%\\^\\&amp;\\*\\(\\)_\\-\\=\\+\\\\\\/\\?\\.\\:\\;\\'\\,]*)?",
                    RegexOptions.IgnoreCase);
            var mactches = regx.Matches(e.Data.Message);
            if (mactches.Count == 0) return;

            foreach (Match match in mactches)
            {
                Console.WriteLine("Found URL: " + match.Value);
                BotConfiguration.Current.Config<List<MatchedURL>>("MatchedUrls").Add(new MatchedURL
                {
                    Channel = e.Data.Channel,
                    DateTime = DateTime.Now,
                    Nick = e.Data.Nick,
                    URL = match.Value
                });

                // get page title
                try
                {
                    var wr = new MyClient {HeadOnly = true};

                    wr.DownloadDataAsync(new Uri(match.Value));
                    wr.DownloadDataCompleted += (sender, args) =>
                    {
                        if (wr.ResponseHeaders != null)
                        {
                            var type = wr.ResponseHeaders["content-type"];
                            if (type.Contains("text/html"))
                            {
                                wr.HeadOnly = false;
                                var text = wr.DownloadString(match.Value);

                                var title =
                                    Regex.Match(text, @"\<title\b[^>]*\>\s*(?<Title>[\s\S]*?)\</title\>",
                                        RegexOptions.IgnoreCase)
                                        .Groups["Title"].Value;

                                if (!string.IsNullOrEmpty(title))
                                    _irc.SendMessage(SendType.Message, e.Data.Channel, "Title: " + title);

                            }
                        }
                    };
                    
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }
    }

    class MyClient : WebClient
    {
        public bool HeadOnly { get; set; }
        protected override WebRequest GetWebRequest(Uri address)
        {
            WebRequest req = base.GetWebRequest(address);
            if (HeadOnly && req.Method == "GET")
            {
                req.Method = "HEAD";
            }
            return req;
        }
    }
}