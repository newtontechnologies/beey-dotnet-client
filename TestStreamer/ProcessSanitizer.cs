using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace TestStreamer
{
    /// <summary>
    /// Process does not close redirected streams, which are unmanaged resource... (at least on linux)
    /// </summary>
    public class ProcessSanitizer : IDisposable
    {
        public ProcessStartInfo StartInfo { get; }


        ProcessStreamSanitizer? stdIn;
        ProcessStreamSanitizer? stdOut;
        ProcessStreamSanitizer? stdErr;

        readonly Stream? stdInStream;
        readonly Stream? stdOutStream;
        readonly Stream? stdErrStream;

        readonly Process process;

        public bool HasExited => disposedValue || process.HasExited;

        public int ExitCode => process.ExitCode;
        public DateTime ExitTime => process.ExitTime;
        public ProcessPriorityClass PriorityClass
        {
            get
            {
                return process.PriorityClass;
            }
            set
            {
                process.PriorityClass = value;
            }
        }

        public void Kill() => process.Kill();
        internal bool WaitForExit(int miliseconds) => process.WaitForExit(miliseconds);
        internal void Refresh() => process.Refresh();

        public ProcessSanitizer(ProcessStartInfo info)
        {
            StartInfo = info ?? throw new ArgumentNullException(nameof(info));
            process = Process.Start(info);

            if (StartInfo.RedirectStandardInput)
                stdInStream = process.StandardInput.BaseStream;
            if (StartInfo.RedirectStandardOutput)
                stdOutStream = process.StandardOutput.BaseStream;
            if (StartInfo.RedirectStandardError)
                stdErrStream = process.StandardError.BaseStream;
        }


        public ProcessStreamSanitizer UseStandardInput()
        {
            if (!StartInfo.RedirectStandardInput)
                throw new InvalidOperationException("StandardInput is not redirected");

            if (stdIn != null)
                throw new InvalidOperationException("standard input can be redirected only once");

            return stdIn ??= new ProcessStreamSanitizer(process.StandardInput.BaseStream);
        }

        public ProcessStreamSanitizer UseStandardOutput()
        {
            if (!StartInfo.RedirectStandardOutput)
                throw new InvalidOperationException("StandardOutput is not redirected");

            if (stdOut != null)
                throw new InvalidOperationException("standard output can be redirected only once");

            return stdOut ??= new ProcessStreamSanitizer(process.StandardOutput.BaseStream);
        }


        public ProcessStreamSanitizer UseStandardError()
        {
            if (!StartInfo.RedirectStandardError)
                throw new InvalidOperationException("StandardError is not redirected");
            if (stdErr != null)
                throw new InvalidOperationException("standard error can be redirected only once");

            return stdErr ??= new ProcessStreamSanitizer(process.StandardError.BaseStream);
        }

        /// <summary>
        /// reads stdout stream using StreamReader
        /// </summary>
        /// <returns></returns>
        public async Task<string> ReadStdOutToEndAsync()
        {
            using var stdout = UseStandardOutput();
            using var reader = new StreamReader(stdout.Stream);
            return await reader.ReadToEndAsync();
        }

        /// <summary>
        /// reads stderr stream using StreamReader
        /// </summary>
        /// <returns></returns>
        public async Task<string> ReadStdErrToEndAsync()
        {
            using var stderr = UseStandardError();
            using var reader = new StreamReader(stderr.Stream);
            return await reader.ReadToEndAsync();
        }



        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                //If we have sanitizer, it will be disposed by sanitizer when needed. 
                //there are buffers in standard streams that can be read after the process is disposed
                //disposing of Process does not dispose the redirected streams

                process?.Dispose();

                //free dangling redirects..
                //if redirect is used by sanitizer, it will be cleaned by it
                if (stdIn is null)
                    stdInStream?.Dispose();
                if (stdOut is null)
                    stdOutStream?.Dispose();
                if (stdErr is null)
                    stdErrStream?.Dispose();
                disposedValue = true;
            }
        }

        ~ProcessSanitizer() => Dispose(false);

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }



        #endregion

    }
}