using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Meebey.SmartIrc4net;

namespace BenBOT
{
    public class BotModulesManager
    {
        public static List<IBotListener> BotListeners;
        public static List<IBotCommand> BotCommands;

        public static void LoadBotListeners(IrcClient irc)
        {
            BotListeners = new List<IBotListener>();

            var botCommands =
                Assembly.GetCallingAssembly()
                    .GetTypes()
                    .Where(t => String.Equals(t.Namespace, "BenBOT.BotListeners", StringComparison.Ordinal))
                    .ToArray();
            foreach (var listener in botCommands.Where(cmd =>
                cmd.GetInterfaces().Contains(typeof(IBotListener))).Select(cmd =>
                    (IBotListener)Activator.CreateInstance(cmd)))
            {
                listener.Init(irc);
                listener.Start();
                BotListeners.Add(listener);
            }
        }

        public static void LoadBotCommands()
        {
            BotCommands = new List<IBotCommand>();

            var botCommands =
                Assembly.GetCallingAssembly()
                    .GetTypes()
                    .Where(t => String.Equals(t.Namespace, "BenBOT.BotCommands", StringComparison.Ordinal))
                    .ToArray();
            foreach (var cmd in botCommands)
            {
                if (cmd.GetInterfaces().Contains(typeof(IBotCommand)))
                    BotCommands.Add((IBotCommand)Assembly.GetCallingAssembly().CreateInstance(cmd.FullName));
            }
        }

        public static IBotCommand GetBotCommand(string command)
        {
            return BotCommands.SingleOrDefault(x => x.CommandsHandled.Contains(command.ToUpper()));
        }
    }
}
