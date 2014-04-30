using BenBOT.Configuration;
using BenBOT.Models;
using Meebey.SmartIrc4net;

namespace BenBOT.BotListeners
{
    public class MatchListener : IBotListener
    {
        private IrcClient _irc;

        public void Init(IrcClient irc)
        {
            _irc = irc;
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

        private void ProcessMessage(IrcEventArgs e)
        {
            BotUser user = BotConfiguration.Current.Settings.GetUser(e.Data.Nick);

            ActionMatch action = BotConfiguration.Current.Settings.CheckActions(e.Data.Message);
            if (action == null) return;

            switch (action.Action.ToUpper())
            {
                case "KICK":
                    if (user != null)
                        if (user.IsAdmin)
                            break;
                    if (_irc.GetChannelUser(e.Data.Channel, e.Data.Nick).IsOp)
                        break;
                    _irc.RfcKick(e.Data.Channel, e.Data.Nick, action.Reason);
                    break;
                case "REPLY":
                    _irc.SendReply(e.Data, action.Reason);
                    break;
            }
        }
    }
}