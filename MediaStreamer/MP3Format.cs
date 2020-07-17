using System;
using System.Collections.Generic;
using System.Text;

namespace MediaStreamer
{
    /// <summary>
    /// MediaFormat for MP3 files
    /// </summary>
    public class MP3Format : MediaFormat
    {
        public override void Seek(uint seconds)
        {
            throw new NotImplementedException();
        }
        public override void Cut(uint seconds)
        {
            throw new NotImplementedException();
        }
        public override uint GetLength(bool total = true)
        {
            throw new NotImplementedException();
        }
    }
}
