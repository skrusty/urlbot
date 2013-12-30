using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BenBOT
{
    public class BotUser
    {

        public BotUser()
        {
            SavedQueries = new List<SavedQuery>();
            CommandHistory = new List<HistoryItem>();
        }

        public string Nick { get; set; }
        public string Pass { get; set; }
        public string Email { get; set; }
        public string DefaultHost { get; set; }
        public bool IsAdmin { get; set; }

        public bool IsGuest { get; set; }

        public List<SavedQuery> SavedQueries { get; set; }
        public List<HistoryItem> CommandHistory { get; set; }

        public int CommandsInLast(int seconds)
        {
            return CommandHistory.Count(x => x.Created >= DateTime.Now.AddSeconds(-seconds));
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
