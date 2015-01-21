using System;
using System.Collections.Generic;
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

        private static void ProcessMessage(IrcEventArgs e)
        {
            BotUser user = BotConfiguration.Current.Settings.GetUser(e.Data.Nick);

            // To be parsed for data
            var regx =
                new Regex(
                    "(http|https)://([\\w+?\\.\\w+])+([a-zA-Z0-9\\~\\!\\@\\#\\$\\%\\^\\&amp;\\*\\(\\)_\\-\\=\\+\\\\\\/\\?\\.\\:\\;\\'\\,]*)?",
                    RegexOptions.IgnoreCase);
            MatchCollection mactches = regx.Matches(e.Data.Message);
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
            }
        }
    }
}