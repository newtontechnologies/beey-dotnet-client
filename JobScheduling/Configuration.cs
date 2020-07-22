using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace JobScheduling
{
    static class Configuration
    {
        private static IConfiguration configuration;

        public static Beey Beey { get => configuration.GetSection("Beey").Get<Beey>(); }

        public static void Load()
        {
            new ConfigurationBuilder()
                  .SetBasePath(Directory.GetCurrentDirectory())
                  .AddXmlFile("Settings.xml", optional: false, reloadOnChange: true)
                  .AddXmlFile("Settings.overrides.xml", optional: true, reloadOnChange: true)
                  .Build();
        }
    }

    class Beey
    {
        public string Url { get; set; }
        public string Login { get; set; }
        public string Password { get; set; }
    }
}
