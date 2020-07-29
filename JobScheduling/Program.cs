using Beey.Client;
using Beey.DataExchangeModel.Messaging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Resources;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace JobScheduling
{
    class Program
    {
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
                Console.Write(prompt);
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
                    Console.WriteLine($"Unknown command. Enter '{commandsKeyword}' if you need help.");
                }
            }

            Console.WriteLine("Scheduler ended.");
        }

        private static void Command_Transcribe(TextWriter console, JobScheduler jobScheduler, string line)
        {
            var split = line.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (split.Length < 3)
            {
                console.WriteLine("Invalid input.");
                console.WriteLine(usage);
                return;
            }
            string url = split[1];
            DateTime date;
            if (DateTime.TryParse(split[2], out date))
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
            Func<Task> job = CreateJob(url, language, duration, projectName, loginToken);
            Action<Exception> onException = ex =>
            {
                // TODO: add better error reporting (e.g. mail)
                Console.WriteLine(ex.Message);
            };

            jobScheduler.ScheduleJob(job, onException, date, repeatInterval);
        }

        private static Func<Task> CreateJob(string url, string language, TimeSpan? duration, string projectName, string loginToken)
        {
            // TODO: log job progress? Where?
            return async () =>
            {
                // Login first to not waste time in case of incorrect login.
                var beey = new BeeyClient(Configuration.Beey.Url);
                if(loginToken == null) {
                    await beey.LoginAsync(Configuration.Beey.Login, Configuration.Beey.Password);
                } else {
                    await beey.LoginAsync(loginToken);
                }

                Stream stream = StartDownloadingStream(url, duration);
                await TranscribeStream(beey, stream, language, projectName);
            };
        }

        private static async Task TranscribeStream(BeeyClient beey, Stream stream, string language, string projectName)
        {
            var project = await beey.CreateProjectAsync(projectName, null);
            await beey.UploadStreamAsync(project.Id, projectName, stream, null, true);
            // Wait for an hour at max.
            int maxWaitingTimeMs = 3600 * 1000;
            int delayMs = 10000;
            int retryCount = maxWaitingTimeMs / delayMs;
            TryValueResult<ProjectProgress> result;
            while ((result = await beey.GetProjectProgressStateAsync(project.Id).TryAsync())
                && !ProcessState.Finished.HasFlag(result.Value.TranscodingState)
                && result.Value.MediaIdentificationState != ProcessState.Completed
                && retryCount > 0)
            {
                await Task.Delay(delayMs);
                retryCount--;
            }

            if (result.Value.TranscodingState == ProcessState.Failed)
                throw new Exception("Trancoding failed.");
            else if (result.Value.TranscodingState != ProcessState.Completed && result.Value.MediaIdentificationState != ProcessState.Completed)
                throw new Exception($"Transcoding did not finish in {(maxWaitingTimeMs / 1000) / 60} minutes.");

            // Wait a bit to let the server be ready to transcribe.
            await Task.Delay(1000);
            await beey.TranscribeProjectAsync(project.Id, language, true, true, true);
        }

        private static Stream StartDownloadingStream(string url, TimeSpan? duration)
        {
            throw new NotImplementedException();
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
