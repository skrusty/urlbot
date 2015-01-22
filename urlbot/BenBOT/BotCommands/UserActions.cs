using System.Linq;
using BenBOT.Configuration;
using Meebey.SmartIrc4net;

namespace BenBOT.BotCommands
{
    public class UserActions : IBotCommand
    {
        public string[] CommandsHandled
        {
            get { return new[] {"!REGISTER"}; }
        }

        public string[] HelpMessage(string command)
        {
            return new[] {"REGISTER"};
        }

        public void ProcessCommand(string[] segments, BotUser user, IrcClient irc, IrcMessageData senderData)
        {
            switch (segments[0].ToUpper())
            {
                case "!REGISTER":
                    try
                    {
                        var newUser =
                            BotConfiguration.Current.Settings.KnownUsers.SingleOrDefault(x => x.Nick == senderData.Nick);
                        if (newUser == null)
                        {
                            newUser = new BotUser
                            {
                                DefaultHost = senderData.Host,
                                Email = segments[1],
                                IsAdmin = false,
                                Nick = senderData.Nick,
                                Pass = segments[2]
                            };
                        }
                        else if (newUser.IsGuest)
                        {
                            newUser.DefaultHost = senderData.Host;
                            newUser.Email = segments[1];
                            newUser.IsAdmin = false;
                            newUser.Nick = senderData.Nick;
                            newUser.Pass = segments[2];
                            newUser.IsGuest = false;
                        }

                        irc.SendMessage(SendType.Message, senderData.Nick,
                            string.Format(
                                "Welcome {0}, we've registered you with the following email address {1}.",
                                senderData.Nick, segments[1]));

                        BotConfiguration.Current.SaveConfig<BotSettings>("config");
                    }
                    catch
                    {
                    }
                    break;
            }
        }
    }
}