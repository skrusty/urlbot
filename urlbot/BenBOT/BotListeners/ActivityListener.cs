using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BenBOT.Configuration;
using Meebey.SmartIrc4net;

namespace BenBOT.BotListeners
{
    public class ActivityListener : IBotListener
    {
        public const string LastActionAttributeKey = "LastActivity";
        public const string LastSpokeAttributeKey = "LastSpokeActivity";
        private IrcClient _irc;

        public void Init(IrcClient irc)
        {
            _irc = irc;
        }

        public void Start()
        {
            _irc.OnChannelMessage += _irc_OnChannelMessage;
            _irc.OnQueryMessage += _irc_OnQueryMessage;
            _irc.OnJoin += _irc_OnJoin;
            _irc.OnPart += _irc_OnPart;
        }

        void _irc_OnPart(object sender, PartEventArgs e)
        {
            LogActivity(new UserActivityItem(e.Channel, "Part", DateTime.Now), e.Who, LastActionAttributeKey);
        }

        void _irc_OnJoin(object sender, JoinEventArgs e)
        {
            LogActivity(new UserActivityItem(e.Channel, "Join", DateTime.Now), e.Who, LastActionAttributeKey);
        }

        private void _irc_OnQueryMessage(object sender, IrcEventArgs e)
        {
            // LogActivity(new UserActivityItem(e.Data.Channel, "Speak", DateTime.Now), e.Data.Nick);
        }

        private void _irc_OnChannelMessage(object sender, IrcEventArgs e)
        {
            LogActivity(new UserActivityItem(e.Data.Channel, "Speak", DateTime.Now), e.Data.Nick, LastSpokeAttributeKey);
        }

        private void LogActivity(UserActivityItem action, string nick, string key)
        {
            var user = BotConfiguration.Current.Settings.GetUser(nick);
            if(user==null)
                return;

            if (user.Attributes.ContainsKey(key))
                user.Attributes[key] = action;
            else
                user.Attributes.Add(key, action);
        }

        public void Stop()
        {
        }
    }

    public class UserActivityItem
    {
        public string Channel { get; set; }
        public DateTime Date { get; set; }
        public string Action { get; set; }

        public UserActivityItem()
        {
        }

        public UserActivityItem(string channel, string action, DateTime date)
        {
            Channel = channel;
            Action = action;
            Date = date;
        }
    }
}
