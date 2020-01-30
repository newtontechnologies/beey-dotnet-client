using System;
using System.Collections.Generic;
using System.Text;

namespace M3U8StreamPusher
{
    public class BeeyConfiguration
    {
        public string Login { get; set; }
        public string Password { get; set; }
        public string URL { get; set; }
        public string TranscriptionLocale { get; set; }

        public bool LogMessages { get; set; }

        public bool LogUpload { get; set; }
    }
}
