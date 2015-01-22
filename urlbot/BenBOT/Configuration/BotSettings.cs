using System.Collections.Generic;
using System.Linq;

namespace BenBOT.Configuration
{
    public class BotSettings
    {
        public BotSettings()
        {
            KnownUsers = new List<BotUser>();
            AutoJoinChannels = new List<string>();
            SMTPSettings = new SMTPSettings();
            IrcNetworkSettings = new IrcNetwork
            {
                ServerName = "Quakenet",
                Server = "irc.quakenet.org",
                Port = 6665,
                Nick = "urlbot"
            };
            //Networks = new List<IrcNetwork>()
            //{
            //    new IrcNetwork()
            //    {
            //        ServerName = "Quakenet",
            //        Server = "irc.quakenet.org",
            //        Port = 6665,
            //        Nick = "urlbot"
            //    }
            //};
        }

        public string BotName { get; set; }
        //public List<IrcNetwork> Networks { get; set; }
        public IrcNetwork IrcNetworkSettings { get; set; }
        public List<string> AutoJoinChannels { get; set; }
        public List<BotUser> KnownUsers { get; set; }
        public SMTPSettings SMTPSettings { get; set; }

        public BotUser GetUser(string nick)
        {
            return KnownUsers.SingleOrDefault(x => x.Nick == nick);
        }

        public List<BotUser> GetAdmins()
        {
            return KnownUsers.Where(x => x.IsAdmin).ToList();
        }
    }

    public class IrcNetwork
    {
        public string Server { get; set; }
        public int Port { get; set; }
        public string ServerName { get; set; }
        public string Nick { get; set; }
    }
}