using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace MediaStreamer
{
    /// <summary>
    /// MediaSource class for downloading videos from web via direct URL.
    /// </summary>
    /// <typeparam name="T">MediaFormat used to perform format specific tasks.</typeparam>
    public class WebSource<T> : MediaSource<T> where T: MediaFormat
    {
        /// <summary>
        /// Initialize new DirectURL class.
        /// </summary>
        /// <param name="url">Direct URL to the media</param>
        /// <param name="offset">*(Optional)* Begin at X seconds.</param>
        /// <param name="length">*(Optional)* Get only first X seconds from the beginning.</param>
        public WebSource(string url, uint offset = 0, uint? length = null)
            : base(url, offset, length)
        {
            stream = new WebClient().OpenRead(url);
            mediaFormat.stream = stream;
        }

        public WebSource(string filename, TimeSpan offset, TimeSpan? length = null)
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
