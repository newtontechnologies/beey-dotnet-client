using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MediaStreamer
{
    /// <summary>
    /// MediaSource working with a local file.
    /// </summary>
    /// <typeparam name="T">MediaFormat used to perform format specific tasks.</typeparam>
    public class LocalSource<T> : MediaSource<T> where T : MediaFormat
    {
        /// <summary>
        /// Initialize new LocalSource class.
        /// </summary>
        /// <param name="filename">Path to the file.</param>
        /// <param name="offset">*(Optional)* Begin at X seconds.</param>
        /// <param name="length">*(Optional)* Get only first X seconds from the beginning.</param>
        public LocalSource(string filename, uint offset = 0, uint? length = null)
            :base(filename, offset, length)
        {
            stream = new FileStream(Uri, FileMode.Open);
            mediaFormat.stream = stream;
        }

        /// <summary>
        /// Initialize a new LocalSource class.
        /// </summary>
        /// <param name="filename">Path to the file</param>
        /// <param name="offset">Time to skip</param>
        /// <param name="length">(optional) Time after which to stop</param>
        public LocalSource(string filename, TimeSpan offset, TimeSpan? length = null)
            :this(filename,(uint)offset.TotalSeconds)
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
