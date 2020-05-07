using System;
using System.Collections.Generic;
using System.Text;

namespace LandeckStreamer
{
    public class BeeyConfiguration
    {
        public string Login { get; set; }
        public string Password { get; set; }
        public string URL { get; set; }
        public string TranscriptionLocale { get; set; }

        public bool LogMessages { get; set; }

        public bool LogUpload { get; set; }
        public bool MessageEcho { get; set; }

        public string LandeckAPIURL { get; set; }

        public string FFmpeg { get; set; }
    }
}
