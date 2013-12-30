using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Serialization;

namespace BenBOT
{
    public class BotConfiguration
    {

        public BotSettings Settings;
        public List<MatchedURL> MatchedURLs = new List<MatchedURL>();

        public void LoadConfig() 
        {
            // Load Config File
            XmlSerializer xs = new XmlSerializer(typeof(BotSettings));
            try
            {
                using (FileStream fs = File.OpenRead("Settings.xml"))
                {
                    Settings = (BotSettings)xs.Deserialize(fs);
                }
            }
            catch
            {
                Settings = new BotSettings();
            }
        }

        public void SaveConfig()
        {
            // Save Application Config File
            XmlSerializer xs = new XmlSerializer(typeof(BotSettings));
            using (FileStream fs = File.Open("Settings.xml", FileMode.Create, FileAccess.Write, FileShare.None))
            {
                lock (Settings)
                {
                    xs.Serialize(fs, Settings);
                }
            }
        }

        public void LoadURLs()
        {
            // Load Config File
            XmlSerializer xs = new XmlSerializer(typeof(List<MatchedURL>));
            try
            {
                using (FileStream fs = File.OpenRead("URLs.xml"))
                {
                    MatchedURLs = (List<MatchedURL>)xs.Deserialize(fs);
                }
            }
            catch
            {
                MatchedURLs = new List<MatchedURL>();
            }
        }

        public void SaveURLs()
        {
            // Save Application Config File
            XmlSerializer xs = new XmlSerializer(typeof(List<MatchedURL>));
            using (FileStream fs = File.Open("URLs.xml", FileMode.Create, FileAccess.Write, FileShare.None))
            {
                lock (MatchedURLs)
                {
                    xs.Serialize(fs, MatchedURLs);
                }
            }
        }

    }
}