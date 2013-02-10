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
        }

        public string Nick { get; set; }
        public string Pass { get; set; }
        public string Email { get; set; }
        public string DefaultHost { get; set; }
        public bool IsAdmin { get; set; }

        public List<SavedQuery> SavedQueries { get; set; }
    }

    public class SavedQuery
    {
        public string Query { get; set; }
        public string Name { get; set; }
    }
}
