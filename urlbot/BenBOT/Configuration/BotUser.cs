using System;
using System.Collections.Generic;
using System.Linq;
using BenBOT.Helpers;
using Meebey.SmartIrc4net;

namespace BenBOT.Configuration
{
    public class BotUser
    {
        public BotUser()
        {
            SavedQueries = new List<SavedQuery>();
            CommandHistory = new List<HistoryItem>();
            Attributes = new SerializableDictionary<string, object>();
        }

        public string Nick { get; set; }
        public string Pass { get; set; }
        public string Email { get; set; }
        public string DefaultHost { get; set; }
        public bool IsAdmin { get; set; }
        public bool IsModerator { get; set; }
        public bool IsGuest { get; set; }

        public DateTime LastSpoke { get; set; }

        public List<SavedQuery> SavedQueries { get; set; }
        public List<HistoryItem> CommandHistory { get; set; }

        public SerializableDictionary<string, object> Attributes { get; set; }

        public int CommandsInLast(int seconds)
        {
            return CommandHistory.Count(x => x.Created >= DateTime.Now.AddSeconds(-seconds));
        }

        public static void BroadcastToAdmins(IrcClient irc, string message, params object[] paramStrings)
        {
            try
            {
                var msg = string.Format(message, paramStrings);
                var admins = BotConfiguration.Current.Settings.GetAdmins();
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
    }

    public class HistoryItem
    {
        public DateTime Created { get; set; }
        public string Command { get; set; }
    }

    public class SavedQuery
    {
        public string Query { get; set; }
        public string Name { get; set; }
    }
}