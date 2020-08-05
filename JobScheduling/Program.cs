using Beey.Client;
using Beey.DataExchangeModel.Messaging;
using Nanogrid.Utils;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace JobScheduling
{
    class Program
    {
        public static bool runningAsScript = false;

        private const string prompt = "> ";
        private const string transcribeKeyword = "transcribe";
        private const string listKeyword = "list";
        private const string cancelKeyword = "cancel";
        private const string commandsKeyword = "commands";
        private const string exitKeyword = "exit";
        private static readonly string usage = $@"COMMANDS:
- '{transcribeKeyword} <media url> <date>' to schedule media transcription.
    - Optional arguments:
        -l <language>      language of the media
        -r <time span>     repeat interval
        -d <duration>      duration of the media
        -n <name>          name of the project in Beey
        -t <token>         token instead of credentials
    - If scheduling finished successfuly, application returns job ID.  
- '{listKeyword}' to show active jobs.
- '{cancelKeyword} <jobId>' to cancel active job.         
- '{commandsKeyword}' to show this text again.
- '{exitKeyword}' to exit.
- press CTRL+C to exit.";

        private static readonly Serilog.ILogger log = Serilog.Log.ForContext<Program>();

        private static readonly HashSet<string> switchesWithArgument = new HashSet<string>()
        {
            "-l", "-d", "-r", "-n", "-t",
            "/l", "/d", "/r", "/n", "/t"
        };
        private static readonly HashSet<string> standaloneSwitches = new HashSet<string>()
        {
        };

        private static async Task<string> ReadLineAsync(CancellationToken token)
        {
            var tcs = new TaskCompletionSource<string>();
            token.Register(() => tcs.SetResult(null));
            var reading = Task.Run(() => Console.ReadLine());

            string line = await Task.WhenAny(reading, tcs.Task).Unwrap();
            if (line == null)
                Console.WriteLine();
            return line;
        }

        static async Task Main(string[] args)
        {
            // open-m3u8 fails when parsing floating point numbers without setting culture, so set it globally
            CultureInfo.DefaultThreadCurrentCulture = CultureInfo.InvariantCulture;

            Serilog.Log.Logger = new LoggerConfiguration()
                .WriteTo.File(DateTime.Now.ToString("yyyy-MM-dd_HHmmss") + ".log")
                .Enrich.FromLogContext()
                .CreateLogger();

            Console.WriteLine("Job Scheduler");
            Configuration.Load();
            if (args.Contains("-?") || args.Contains("/?"))
                Console.WriteLine(usage);
            var jobScheduler = new JobScheduler();
            var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (s, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
            };
            while (!cts.IsCancellationRequested)
            {
                if (args.Length == 0)
                {
                    Console.Write(prompt);
                } else {
                    runningAsScript = true;
                }
                    

                string line = await ReadLineAsync(cts.Token);

                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }
                if (line == exitKeyword)
                {
                    cts.Cancel();
                }
                else if (line.StartsWith(transcribeKeyword))
                {
                    Command_Transcribe(Console.Out, jobScheduler, line);
                }
                else if (line == listKeyword)
                {
                    Command_List(Console.Out, jobScheduler);
                }
                else if (line == commandsKeyword)
                {
                    Console.WriteLine(usage);
                }
                else if (line.StartsWith(cancelKeyword))
                {
                    Command_Cancel(Console.Out, jobScheduler, line);
                }
                else
                {
                    if(runningAsScript)
                    {
                        Console.WriteLine("[FATAL] Unknown command.");
                        Environment.Exit(-1);
                    } else
                        Console.WriteLine($"Unknown command. Enter '{commandsKeyword}' if you need help.");
                }
            }

            Console.WriteLine("Scheduler ended.");
        }

        private static void Command_Transcribe(TextWriter console, JobScheduler jobScheduler, string line)
        {
            var split = line.Split('"')
                .Select((s, i)
                    => i % 2 == 0
                        ? s.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                        : new string[] { s })
                .SelectMany(s => s).ToArray();

            if (split.Length < 3)
            {
                console.WriteLine("Invalid input.");
                console.WriteLine(usage);
                return;
            }
            string url = split[1];
            DateTime date;
            if (!DateTime.TryParse(split[2], out date))
            {
                console.WriteLine($"Invalid date '{split[2]}'.");
                return;
            }

            TimeSpan? duration = null;
            TimeSpan? repeatInterval = null;
            string projectName = null;
            string language = null;
            string loginToken = null;

            for (int i = 3; i < split.Length; i++)
            {
                if (!split[i].StartsWith('-') && !split[i].StartsWith('/'))
                {
                    console.WriteLine($"Expected switch instead of '{split[i]}'.");
                    return;
                }
                else if (switchesWithArgument.Contains(split[i])
                    && (split.Length <= i + 1 || split[i + 1].StartsWith('-') || split[i + 1].StartsWith('/')))
                {
                    console.WriteLine($"Missing argument for switch '{split[i]}'.");
                    return;
                }
                switch (split[i])
                {
                    case "-r":
                    case "/r":
                        i++;
                        if (TimeSpan.TryParse(split[i], out var r))
                            repeatInterval = r;
                        else
                        {
                            console.WriteLine($"Invalid time span '{split[i]}'.");
                            return;
                        }
                        break;
                    case "-d":
                    case "/d":
                        i++;
                        if (TimeSpan.TryParse(split[i], out var d))
                            duration = d;
                        else
                        {
                            console.WriteLine($"Invalid time span '{split[i]}'.");
                            return;
                        }
                        break;
                    case "-n":
                    case "/n":
                        i++;
                        projectName = split[i];
                        break;
                    case "-t":
                    case "/t":
                        i++;
                        loginToken = split[i];
                        break;
                    case "-l":
                    case "/l":
                        i++;
                        language = split[i];
                        break;
                    default:
                        console.WriteLine($"Invalid switch '{split[i]}'.");
                        return;
                }
            }

            projectName = projectName ?? $"scheduled_{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")}";
            language = language ?? "cs-CZ";

            if (runningAsScript)
                Console.WriteLine("Waiting..");

            Func<Task> job = CreateJob(url, language, duration, projectName, loginToken);
            Action<int, Exception> onException = (id, ex) =>
            {
                log.Error(ex.Message, $"Error when transcribing job {id}.");
                Console.WriteLine("[FATAL] Error while transcribing" + ex.ToString());
            };

            jobScheduler.ScheduleJob(job, onException, date, repeatInterval);
        }

        private static Func<Task> CreateJob(string url, string language, TimeSpan? duration, string projectName, string loginToken)
        {
            return async () =>
            {
                // Login first to not waste time in case of incorrect login.

                if(runningAsScript)
                    Console.WriteLine("Login to beey...");

                var beey = new BeeyClient(Configuration.Beey.Url);
                if (loginToken == null)
                {
                    await beey.LoginAsync(Configuration.Beey.Login, Configuration.Beey.Password);
                }
                else
                {
                    await beey.LoginAsync(loginToken);
                }


                if(runningAsScript)
                    Console.WriteLine("Start download...");

                var (stream, downloading) = StartDownloadingStream(url, duration);
                try
                {
                    if (runningAsScript)
                        Console.WriteLine("Transcribing...");

                    var transcribing = TranscribeStreamAsync(beey, stream, language, projectName);
                    await Task.WhenAny(downloading, transcribing).Unwrap();
                    await downloading;
                    await transcribing;

                    if(runningAsScript)
                        Console.WriteLine("Finished.");
                    Environment.Exit(0);
                } catch(Exception ex) {
                    if(runningAsScript)
                        Console.WriteLine("[FATAL] Exception while working: " + ex.ToString());
                }
                finally
                {
                    stream.Dispose();
                }
            };
        }

        private static async Task TranscribeStreamAsync(BeeyClient beey, Stream stream, string language, string projectName)
        {
            var project = await beey.CreateProjectAsync(projectName, null);
            if(runningAsScript)
                Console.WriteLine("[CALLBACK]|projectId|"+project.Id);
            var cts = new CancellationTokenSource();
            var uploading = beey.UploadStreamAsync(project.Id, projectName, stream, null, false, cts.Token);
            // Wait for an hour at max.
            int maxWaitingTimeMs = 3600 * 1000;
            int delayMs = 10000;
            int retryCount = maxWaitingTimeMs / delayMs;
            TryValueResult<ProjectProgress> result;
            // Apparently, progress in backend can be created a bit late, so wait for a bit.
            await Task.Delay(2000);
            while ((result = await beey.GetProjectProgressStateAsync(project.Id).TryAsync())
                && !ProcessState.Finished.HasFlag(result.Value.FileIndexingState)
                && result.Value.MediaIdentificationState != ProcessState.Completed
                && retryCount > 0)
            {
                await Task.Delay(delayMs);
                retryCount--;
            }

            if (result.Value.FileIndexingState == ProcessState.Failed)
            {
                cts.Cancel();
                throw new Exception("File indexing failed.");
            }
            else if (result.Value.FileIndexingState != ProcessState.Completed && result.Value.MediaIdentificationState != ProcessState.Completed)
            {
                cts.Cancel();
                throw new Exception($"File indexing did not finish in {(maxWaitingTimeMs / 1000) / 60} minutes.");
            }

            // Wait a bit to let the server be ready to transcribe.
            await Task.Delay(2000);
            try
            {
                await beey.TranscribeProjectAsync(project.Id, language, true, true, true);
            }
            catch (Exception)
            {
                cts.Cancel();
                throw;
            }

            await uploading;
        }

        private static (Stream Stream, Task Downloading) StartDownloadingStream(string url, TimeSpan? duration)
        {
            var stream = new BufferingStream();
            var task = DownloadStreamAsync(stream, url, duration);
            return (stream, task);
        }

        private static async Task DownloadStreamAsync(BufferingStream destination, string url, TimeSpan? duration)
        {
            string tmpFilename = $"tmp_{Guid.NewGuid()}.out";
            try
            {
                var ffmpegInfo = new ProcessStartInfo
                {
                    FileName = Configuration.FFmpeg.Path,
                    Arguments = string.Format(Configuration.FFmpeg.StreamArgs, url, duration.HasValue ? "-t " + duration.ToString() : "", tmpFilename),
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                };
                var process = Process.Start(ffmpegInfo);
                var stdoutLastChange = DateTime.Now;
                var readingOutput = Task.Run(async () =>
                {
                    byte[] bfr = new byte[4096];
                    while (!process.HasExited)
                    {
                        stdoutLastChange = DateTime.Now;
                        var cnt = await process.StandardOutput.BaseStream.ReadAsync(bfr.AsMemory());
                        if (cnt == 0)
                            break;
                        await destination.WriteAsync(bfr, 0, cnt);
                    }
                });
                var readingError = process.StandardError.ReadToEndAsync();

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

        private static void Command_List(TextWriter console, JobScheduler jobScheduler)
        {
            var jobs = jobScheduler.ListJobs();
            if (!jobs.Any())
            {
                console.WriteLine("No active jobs.");
                return;
            }

            foreach (var job in jobs)
            {
                string id = job.Key.ToString();
                string date = job.Value.DateToRun.ToString("yyyy-MM-dd HH:mm:ss");
                string repeatInterval = job.Value.RepeatInterval.HasValue
                    ? "(" + job.Value.RepeatInterval.Value.ToString() + ")"
                    : "";
                console.WriteLine($"\t{id}: {date}{repeatInterval}");
            }
        }

        private static void Command_Cancel(TextWriter console, JobScheduler jobScheduler, string line)
        {

            if (int.TryParse(line.Substring(cancelKeyword.Length).Trim(), out int jobId))
            {
                if (jobScheduler.CancelJob(jobId))
                    console.WriteLine($"Job '{jobId}' canceled.");
                else
                    console.WriteLine($"Job '{jobId}' does not exist.");
            }
            else
            {
                console.WriteLine("Invalid job ID.");
            }
        }
    }
}
