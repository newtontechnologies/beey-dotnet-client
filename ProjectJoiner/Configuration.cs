using Microsoft.Extensions.Configuration;
using System;
using System.IO;

namespace ProjectJoiner
{
    static class Configuration
    {
        private static IConfigurationRoot config;

        public static ProjectJoinerConfiguration ProjectJoiner
            => config.GetSection("ProjectJoiner").Get<ProjectJoinerConfiguration>();

        public static void Load()
        {
            config = new ConfigurationBuilder()
                  .SetBasePath(Directory.GetCurrentDirectory())
                  .AddXmlFile("Settings.xml", optional: false, reloadOnChange: true)
                  .AddXmlFile("Settings.overrides.xml", optional: true, reloadOnChange: true)
                  .Build();
        }
    }

    class ProjectJoinerConfiguration
    {
        public string? Url { get; set; }
        public string? Login { get; set; }
        public string? Password { get; set; }

        public string? WinFFMpeg { get; set; }
        public string? WinFFProbe { get; set; }
        public string? UnixFFMpeg { get; set; }
        public string? UnixFFProbe { get; set; }
        public string? MergeParams { get; set; }
    }
}
