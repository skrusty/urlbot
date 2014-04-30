using BenBOT.Configuration;
using Meebey.SmartIrc4net;

namespace BenBOT
{
    public interface IBotCommand
    {
        string[] CommandsHandled { get; }
        string[] HelpMessage(string command);
        void ProcessCommand(string[] segments, BotUser user, IrcClient irc, IrcMessageData senderData);
    }
}