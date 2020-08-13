using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

#nullable enable
namespace TestStreamer
{
    /// <summary>
    /// Process does not close redirected streams, which are unmanaged resource... (at least on linux)
    /// close & dispose underlying stream with dispose or finalize of this class
    /// </summary>
    public class ProcessStreamSanitizer : IDisposable
    {
        private static readonly Serilog.ILogger logger = Serilog.Log.ForContext<ProcessStreamSanitizer>();
        public ProcessStreamSanitizer(Stream target)
        {
            Stream = target ?? throw new ArgumentNullException(nameof(target));
        }

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        public Stream Stream { get; }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                }

                // TODO: free unmanaged resources (unmanaged objects) and override a finalizer below.
                // TODO: set large fields to null.
                try
                {
                    Stream.Dispose();
                }
                catch (IOException e) when (e.Message == "Broken pipe" && RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                {

                }
                catch (Exception e)
                {
                    logger.Error(e, "exception when diposing process pipe");
                }
                finally
                {
                    disposedValue = true;
                }
            }
        }

        ~ProcessStreamSanitizer()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(false);
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion

    }
}
