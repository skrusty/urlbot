using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BenBOT
{
    public class BotUser
    {
        public string Nick { get; set; }
        public string Pass { get; set; }
        public string Email { get; set; }
        public string DefaultHost { get; set; }
        public bool IsAdmin { get; set; }
    }
}
