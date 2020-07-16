using CommandLine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace LandeckStreamer
{
    public class CommandLineOptions
    {
        [Option('c', HelpText = "channel name from packager ini", Required = true)]
        public string Channel { get; set; }

        [Option('s', HelpText = "start time", Required = false)]
        public DateTime Start { get; set; } = DateTime.Now;

        [Option('t', HelpText = "title", Required = true)]
        public string Title { get; set; }

        [Option('l', HelpText = "length to download", Required = false)]
        public TimeSpan Length { get; set; } = TimeSpan.FromMinutes(5);
    }

}
