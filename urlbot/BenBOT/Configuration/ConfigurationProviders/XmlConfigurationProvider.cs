using System.IO;
using System.Xml.Serialization;

namespace BenBOT.Configuration.ConfigurationProviders
{
    public class XmlConfigurationProvider : IConfigurationProvider
    {
        public void SaveConfiguration<T>(object configObject, string configName)
        {
            // Save Application Config File
            var xs = new XmlSerializer(typeof (T));
            using (var fs = File.Open(configName + ".xml", FileMode.Create, FileAccess.Write, FileShare.None))
            {
                lock (configObject)
                {
                    xs.Serialize(fs, configObject);
                }
            }
        }

        public T LoadConfiguration<T>(string configName) where T : class
        {
            // Load Config File
            var xs = new XmlSerializer(typeof (T));
            try
            {
                using (var fs = File.OpenRead(configName + ".xml"))
                {
                    return (T) xs.Deserialize(fs);
                }
            }
            catch
            {
                return null;
            }
        }
    }
}