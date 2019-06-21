using Microsoft.Extensions.Configuration;
using System;
using System.IO;

namespace SpeakerDbUpdater
{
    static class Configuration
    {
        private static IConfigurationRoot config;

        public static SpeakerDbUpdaterConfiguration SpeakerDbUpdater
            => config.GetSection("SpeakerDbUpdater").Get<SpeakerDbUpdaterConfiguration>();

        public static void Load()
        {            
            config = new ConfigurationBuilder()
                  .SetBasePath(Directory.GetCurrentDirectory())
                  .AddXmlFile("Settings.xml", optional: false, reloadOnChange: true)
                  .AddXmlFile("Settings.overrides.xml", optional: true, reloadOnChange: true)
                  .Build();
        }
    }

    class SpeakerDbUpdaterConfiguration
    {
        public string? Url { get; set; }
        public string? Login { get; set; }
        public string? Password { get; set; }

        public bool UpdateDb { get; set; }
        public string? IniPath { get; set; }
        public bool InsertOnlyNew { get; set; }

        public bool RemoveDuplicities { get; set; }

        public SpeakerDbUpdaterConfiguration()
        {
            UpdateDb = true;
            InsertOnlyNew = true;
            RemoveDuplicities = true;
        }
    }
}
