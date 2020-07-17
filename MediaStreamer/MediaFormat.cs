using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

namespace MediaStreamer
{
    /// <summary>
    /// Abstract class, performs format specific tasks.
    /// </summary>
    public abstract class MediaFormat //TODO: Change the methods; will do it after i start working on real formatter
    {
        /// <summary>
        /// Data stream
        /// </summary>
        public Stream stream;

        /// <summary>
        /// Seek forward by X seconds.
        /// </summary>
        /// <param name="seconds">Seconds</param>
        public abstract void Seek(uint seconds);

        /// <summary>
        /// Cut out last X seconds.
        /// </summary>
        /// <param name="seconds">Seconds</param>
        public abstract void Cut(uint seconds);

        /// <summary>
        /// Gets length of the stream in seconds.
        /// </summary>
        /// <param name="total">False to get only remaning seconds to the end. (Exclude already read part)</param>
        /// <returns>Seconds</returns>
        public abstract uint GetLength(bool total = true);

        /// <summary>
        /// Gets remaining seconds to the end.
        /// </summary>
        /// <returns>Seconds</returns>
        public uint Remaining()
        {
            return GetLength(false);
        }
    }
}
