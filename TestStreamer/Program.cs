using Beey.Client;
using Beey.DataExchangeModel.Messaging;
using Nanogrid.Utils;
using Serilog;
using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace TestStreamer
{
    class Program
    {
        private static readonly ILogger log = Log.ForContext<Program>();

        static async Task Main(string[] args)
        {
            Log.Logger = new LoggerConfiguration()
                .WriteTo.File(DateTime.Now.ToString("yyyy-MM-dd_HHmmss") + ".log")
                .WriteTo.Console()
                .Enrich.FromLogContext()
                .CreateLogger();

            string path = args[0];
            TimeSpan duration = TimeSpan.Parse(args[1]);
            string language = args[2];
            var projectName = $"fake stream {DateTime.Now.ToLongTimeString()}";

            // Login first to not waste time in case of incorrect login.
            var beey = new BeeyClient(Configuration.Beey.Url);
            await beey.LoginAsync(Configuration.Beey.Login, Configuration.Beey.Password);

            var stream = new BufferingStream();
            try
            {
                var streaming = StreamFileAsync(stream, path, duration);
                var transcribing = BeeyHelper.UploadAndTranscribe(beey, stream, null, false, projectName, language);
                await Task.WhenAny(streaming, transcribing).Unwrap();
                await streaming;
                await transcribing;
            }
            finally
            {
                stream.Dispose();
            }
        }

        private static async Task StreamFileAsync(BufferingStream destination, string path, TimeSpan? duration)
        {
            string tmpFilename = $"tmp_{Guid.NewGuid()}.out";
            try
            {
                var ffmpegInfo = new ProcessStartInfo
                {
                    FileName = Configuration.FFmpeg.Path,
                    Arguments = string.Format(Configuration.FFmpeg.StreamArgs, path, duration.HasValue ? "-t " + duration.ToString() : "", tmpFilename),
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                };
                log.Information("Starting FFmpeg.");
                var process = new ProcessSanitizer(ffmpegInfo);
                var stdoutLastChange = DateTime.Now;
                var readingOutput = Task.Run(async () =>
                {
                    byte[] bfr = new byte[4096];
                    using (var output = process.UseStandardOutput())
                    {
                        while (!process.HasExited)
                        {
                            stdoutLastChange = DateTime.Now;
                            var cnt = await output.Stream.ReadAsync(bfr.AsMemory());
                            if (cnt == 0)
                                break;
                            await destination.WriteAsync(bfr, 0, cnt);
                        }
                    }
                });
                var readingError = process.ReadStdErrToEndAsync();

                while (!process.HasExited && stdoutLastChange + TimeSpan.FromSeconds(35) > DateTime.Now)
                {
                    await Task.Delay(1000);
                    process.Refresh();
                }

                if (!process.HasExited)
                {
                    process.Kill();
                    throw new Exception("FFmpeg got stuck.");
                }
                else if (process.ExitCode != 0)
                {
                    var fferr = await readingError;
                    throw new Exception($"FFmpeg exited with error {fferr}.");
                }
                else
                {
                    await readingOutput;
                    var fferr = await readingError;
                    if (fferr.Contains("audio:0kB")) //sometimes ffmpeg returns 0, but does not extract any audio...
                    {
                        throw new Exception($"FFmpeg finished, but produced no audio with error {fferr}.");
                    }
                }
            }
            finally
            {
                log.Information("FFmpeg finished.");
                destination.CompleteWrite();
                bool deleted = false;
                int delayTimeSeconds = 1;
                int delayTimeMaxSeconds = 10;
                while (!deleted)
                {
                    try
                    {
                        if (File.Exists(tmpFilename))
                            File.Delete(tmpFilename);
                        deleted = true;
                    }
                    catch (Exception)
                    {
                        log.Warning("Error when deleting file {file}. Retrying in {seconds} seconds.", tmpFilename, delayTimeSeconds);
                        await Task.Delay(delayTimeSeconds);
                        delayTimeSeconds++;
                        if (delayTimeSeconds >= delayTimeMaxSeconds)
                        {
                            log.Fatal("File {file} cannot be deleted.", tmpFilename);
                            break;
                        }
                    }
                }
            }

            /* TODO: reimplement M3u8MediaSource and M3u8Media to use FFmpeg
            var mediaSource = new M3u8MediaSource(new Uri(url));
            var media = await mediaSource.LoadMediaAsync(duration);
            await media.CopyToAsync(destination);
            destination.CompleteWrite();
            */
        }
    }
}
