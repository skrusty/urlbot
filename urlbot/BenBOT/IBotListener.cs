using Meebey.SmartIrc4net;

namespace BenBOT
{
    public interface IBotListener
    {
        void Init(IrcClient irc);
        void Start();
        void Stop();
    }
}