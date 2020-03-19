using Microsoft.Extensions.Configuration;
using System;
using System.IO;

namespace ProjectMerger
{
    static class Configuration
    {
        private static IConfigurationRoot config;

        public static ProjectMergerConfiguration ProjectMerger
            => config.GetSection("ProjectMerger").Get<ProjectMergerConfiguration>();

        public static void Load()
        {
            config = new ConfigurationBuilder()
                  .SetBasePath(Directory.GetCurrentDirectory())
                  .AddXmlFile("Settings.xml", optional: false, reloadOnChange: true)
                  .AddXmlFile("Settings.overrides.xml", optional: true, reloadOnChange: true)
                  .Build();
        }
    }

    class ProjectMergerConfiguration
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
