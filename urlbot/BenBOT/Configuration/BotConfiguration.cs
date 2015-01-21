using System;
using System.Collections.Generic;

namespace BenBOT.Configuration
{
    public class BotConfiguration
    {
        private readonly Dictionary<string, object> _configObjects;
        private readonly IConfigurationProvider _configProvider;

        public BotSettings Settings;

        public BotConfiguration(IConfigurationProvider configProvider)
        {
            _configObjects = new Dictionary<string, object>();
            _configProvider = configProvider;

            // pre load the BotSettings class
            RegisterConfig<BotSettings>("config", new BotSettings());
        }

        public static BotConfiguration Current { get; set; }

        /// <summary>
        ///     returns a specific configuration element
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="configName"></param>
        /// <returns></returns>
        public T Config<T>(string configName)
        {
            if (!_configObjects.ContainsKey(configName))
                throw new Exception("Config not found");

            return (T) _configObjects[configName];
        }

        public void RegisterConfig<T>(string configName, object configObject) where T : class
        {
            // try and load a config if it already exists
            var config = _configProvider.LoadConfiguration<T>(configName);

            // if it doesn't exist, pre-populate it with the configObject
            if (config == null)
                _configObjects.Add(configName, configObject);
            else
                _configObjects.Add(configName, config);
        }

        public void SaveConfig<T>(string configName) where T : class
        {
            if (!_configObjects.ContainsKey(configName))
                throw new Exception("Config not found");
            var conf = _configObjects[configName];

            _configProvider.SaveConfiguration<T>(conf, configName);
        }

        public T LoadConfig<T>(string configName) where T : class
        {
            if (!_configObjects.ContainsKey(configName))
                throw new Exception("Config not found");

            return _configProvider.LoadConfiguration<T>(configName);
        }
    }
}