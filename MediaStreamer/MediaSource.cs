using System;
using System.IO;

namespace MediaStreamer
{
    /// <summary>
    /// Main MediaStream abstract class
    /// </summary>
    /// <typeparam name="T">MediaFormat used to perform format specific tasks.</typeparam>
    public abstract class MediaSource<T> where T: MediaFormat
    {
        /// <summary>
        /// Uri from which media are to be downloaded.
        /// </summary>
        public string Uri { protected set; get; }
        /// <summary>
        /// Begin at X seconds.
        /// </summary>
        public uint Offset { protected set; get; }
        /// <summary>
        /// Get only X seconds from the beginning.
        /// </summary>
        public uint? Length { protected set; get; }

        /// <summary>
        /// Format specific tasks handling.
        /// </summary>
        protected MediaFormat mediaFormat;

        /// <summary>
        /// Internal data stream.
        /// </summary>
        protected Stream stream;

        /// <summary>
        /// Initialize a new MediaSource.
        /// </summary>
        /// <param name="uri">Filename/URL to media</param>
        /// <param name="offset">*(optional)* Begin at X seconds.</param>
        /// <param name="length">*(optional)* Get only X seconds from the beginning. (null = to the end)</param>
        public MediaSource(string uri, uint offset = 0, uint? length = null)
        {
            Uri = uri;
            Offset = offset;
            Length = length;
            mediaFormat = (T)Activator.CreateInstance(typeof(T));

            // TODO: Put this somewhere else; not in the constructor.
            mediaFormat.Seek(offset);
            if (length != null)
                mediaFormat.Cut(mediaFormat.GetLength(false) - (uint)length);
        }

        /// <summary>
        /// Initialize a new MediaSource
        /// </summary>
        /// <param name="uri">Filename/URL to media</param>
        /// <param name="offset">Time to skip</param>
        /// <param name="length">(optional) Time after which to stop</param>
        public MediaSource(string uri, TimeSpan offset, TimeSpan? length = null)
            : this(uri, (uint)offset.TotalSeconds)
        {
            if (length != null)
                Length = (uint)length.Value.TotalSeconds;
        }

        /// <summary>
        /// Returns the data stream.
        /// </summary>
        /// <returns>Media data stream</returns>
        public abstract Stream GetStream();

        /// <summary>
        /// Jump forward by X seconds.
        /// </summary>
        /// <param name="seconds">Num. of seconds</param>
        protected void Seek(uint seconds)
        {
            mediaFormat.Seek(seconds);
        }
    }
}
