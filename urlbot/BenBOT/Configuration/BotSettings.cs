using System.Collections.Generic;
using System.Linq;
using BenBOT.Models;

namespace BenBOT.Configuration
{
    public class BotSettings
    {
        public BotSettings()
        {
            BotName = "urlbot";
            KnownUsers = new List<BotUser>();
            AutoJoinChannels = new List<string>();
            SMTPSettings = new SMTPSettings();
            MatchActions = new List<ActionMatch>();
        }

        public string BotName { get; set; }
        public List<string> AutoJoinChannels { get; set; }
        public List<BotUser> KnownUsers { get; set; }

        public List<ActionMatch> MatchActions { get; set; }

        public SMTPSettings SMTPSettings { get; set; }

        public BotUser GetUser(string nick)
        {
            return KnownUsers.SingleOrDefault(x => x.Nick == nick);
        }

        public List<BotUser> GetAdmins()
        {
            return KnownUsers.Where(x => x.IsAdmin).ToList();
        }

        public ActionMatch CheckActions(string strToMatch)
        {
            return MatchActions.FirstOrDefault(x => strToMatch.Contains(x.MatchString));
        }
    }
}