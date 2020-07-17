using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace MediaStreamer
{
    /// <summary>
    /// MediaStream for downloading videos from YouTube
    /// </summary>
    /// <typeparam name="T">Formatter</typeparam>
    public class YouTubeSource : MediaSource<MP3Format> //TODO: Change MP3 to something more appropriate
    {
        /// <summary>
        /// Initialize new YouTubeSource class.
        /// </summary>
        /// <param name="url">YouTube url</param>
        /// <param name="offset">First X seconds to skip.</param>
        /// <param name="length">Read only X seconds from start.</param>
        public YouTubeSource(string url, uint offset = 0, uint? length = null)
           : base(url, offset, length)
        {
            // TODO: Parse video from YouTube here
            mediaFormat.stream = stream;
        }

        public YouTubeSource(string filename, TimeSpan offset, TimeSpan? length = null)
            : this(filename, (uint)offset.TotalSeconds)
        {
            if (length != null)
                Length = (uint)length.Value.TotalSeconds;
        }
        public override Stream GetStream()
        {
            return stream;
        }
    }
}
