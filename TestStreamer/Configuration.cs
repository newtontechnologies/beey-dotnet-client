using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace TestStreamer
{
    static class Configuration
    {
        private static IConfiguration configuration;

        public static Beey Beey { get => configuration.GetSection("Beey").Get<Beey>(); }
        public static FFmpeg FFmpeg { get => configuration.GetSection("FFmpeg").Get<FFmpeg>(); }

        static Configuration()
        {
            configuration = new ConfigurationBuilder()
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
    class FFmpeg
    {
        public string PathWin { get; set; }
        public string PathUnix { get; set; }
        public string StreamArgs { get; set; }

        public string Path { get => RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? PathWin : PathUnix; }

    }
}
