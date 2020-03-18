using Beey.Client;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TranscriptionCore;

namespace ProjectJoiner
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            Configuration.Load();
            if (Configuration.ProjectJoiner.Url == null
                || Configuration.ProjectJoiner.Login == null
                || Configuration.ProjectJoiner.Password == null)
            {
                Log.Fatal("Missing settings: Url, Login, Password");
                return -1;
            }

            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .CreateLogger();

            var cts = new CancellationTokenSource();

            string language = "cs-CZ";
            int projectId1 = 2717;
            int projectId2 = 2718;
            var beey = new BeeyClient(Configuration.ProjectJoiner.Url);
            Log.Information("Logging in");
            await beey.LoginAsync(Configuration.ProjectJoiner.Login, Configuration.ProjectJoiner.Password, cts.Token);

            Log.Information("Downloading projects");
            var project1 = await beey.GetProjectAsync(projectId1, cts.Token);
            var project2 = await beey.GetProjectAsync(projectId2, cts.Token);

            if (project1.RecordingId == null || project2.RecordingId == null)
            {
                Log.Fatal("At least one of the projects does not contain recording.");
                return -1;
            }
            if ((project1.OriginalTrsxId == null && project1.CurrentTrsxId == null)
                || (project2.OriginalTrsxId == null && project2.CurrentTrsxId == null))
            {
                Log.Fatal("At least one of the projects does not contain trsx.");
                return -1;
            }

            Log.Information("Downloading files");
            var recordingTask1 = beey.DownloadFileAsync(project1.Id, project1.RecordingId.Value, cts.Token);
            var recordingTask2 = beey.DownloadFileAsync(project2.Id, project2.RecordingId.Value, cts.Token);
            var trsxTask1 = beey.DownloadTrsxAsync(project1.Id, project1.CurrentTrsxId ?? project1.OriginalTrsxId!.Value, cts.Token);
            var trsxTask2 = beey.DownloadTrsxAsync(project2.Id, project2.CurrentTrsxId ?? project2.OriginalTrsxId!.Value, cts.Token);

            try
            {
                await Task.WhenAll(recordingTask1, recordingTask2, trsxTask1, trsxTask2);
                Log.Information("Done");
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Error when downloading files.");
                return -1;
            }

            string resultPath = "result.mp4";
            Task<FileStream>? mergeAudioTask = null;
            try
            {
                using (var joinedTrsx = new MemoryStream())
                {
                    Log.Information("Merging files");
                    mergeAudioTask = MergeAudioFilesAsync(resultPath, await recordingTask1, "audio1.file", await recordingTask2, "audio2.file", cts.Token);
                    MergeTrsxFiles(joinedTrsx, await trsxTask1, await trsxTask2);
                    using (var joinedFile = await mergeAudioTask)
                    {
                        Log.Information("Creating project");
                        var joinedProj = await beey.CreateProjectAsync(project1.Name + " + " + project2.Name, "", cts.Token);
                        joinedProj = await beey.UploadTrsxAsync(joinedProj.Id, joinedProj.AccessToken, "joined.trsx", joinedTrsx, true, cts.Token);
                        await beey.UploadStreamAsync(joinedProj.Id, joinedFile.Name, joinedFile, joinedFile.Length, language, false, cts.Token);
                    }
                }
                Log.Information("Finished successfuly");
            }
            catch (Exception)
            {
                return -1;
            }
            finally
            {
                if (mergeAudioTask != null)
                {
                    try
                    {
                        (await mergeAudioTask)?.Dispose();
                    }
                    catch { }
                }

                if (File.Exists(resultPath))
                {
                    File.Delete(resultPath);
                }
            }

            return 0;
        }

        private static bool SkipEmpty(IEnumerator<TranscriptionParagraph> enumerator)
        {
            var blink = TimeSpan.FromMilliseconds(100);
            bool result = true;
            while (result = enumerator.MoveNext())
            {
                var noises = enumerator.Current.Children.Where(ch => ch.InnerText.Contains("n::")).ToList();
                foreach (var noise in noises)
                {
                    enumerator.Current.Children.Remove(noise);
                }

                if (enumerator.Current.Children.Count == 0) { continue; }

                enumerator.Current.Begin = enumerator.Current.Children.First().Begin;
                enumerator.Current.End = enumerator.Current.Children.Last().End;

                if (enumerator.Current.Length > blink) { break; }
            }

            return result;
        }
        private static TimeSpan GetBeforeEnd(TranscriptionElement element)
            => element.End - TimeSpan.FromMilliseconds(1);
        private static void MergeTrsxFiles(Stream result, Stream stream1, Stream stream2)
        {
            var trsx1 = Transcription.Deserialize(stream1);
            var trsx2 = Transcription.Deserialize(stream2);
            var mergedTrsx = new Transcription();

            var enumerator1 = trsx1.EnumerateParagraphs().GetEnumerator();
            var enumerator2 = trsx2.EnumerateParagraphs().GetEnumerator();
            var first = (Enumerator: enumerator1, CanRead: SkipEmpty(enumerator1));
            var second = (Enumerator: enumerator2, CanRead: SkipEmpty(enumerator2));
            while (first.CanRead || second.CanRead)
            {
                if (!first.CanRead)
                {
                    mergedTrsx.Add(second.Enumerator.Current);
                    second.CanRead = SkipEmpty(second.Enumerator);
                }
                else if (!second.CanRead)
                {
                    mergedTrsx.Add(first.Enumerator.Current);
                    first.CanRead = SkipEmpty(first.Enumerator);
                }
                else
                {
                    ref var earlier = ref first;
                    ref var later = ref second;

                    if (first.Enumerator.Current.Begin > second.Enumerator.Current.Begin)
                    {
                        earlier = ref second;
                        later = ref first;
                    }

                    if (earlier.Enumerator.Current.End > later.Enumerator.Current.Begin)
                    {
                        var half = (earlier.Enumerator.Current.End - later.Enumerator.Current.Begin) / 2;
                        earlier.Enumerator.Current.End -= half;
                        later.Enumerator.Current.Begin += half;
                    }
                    mergedTrsx.Add(earlier.Enumerator.Current);
                    earlier.CanRead = SkipEmpty(earlier.Enumerator);
                }
            }

            mergedTrsx.Serialize(result);
            result.Seek(0, SeekOrigin.Begin);
        }

        private static async Task<FileStream> MergeAudioFilesAsync(string resultPath, Stream stream1, string filePath1,
            Stream stream2, string filePath2,
            CancellationToken token)
        {
            using (var fs1 = File.Create(filePath1))
            using (var fs2 = File.Create(filePath2))
            {
                var task1 = stream1.CopyToAsync(fs1);
                var task2 = stream2.CopyToAsync(fs2);
                await Task.WhenAll(task1, task2);
            }

            Log.Information("Starting audio merge");
            string parameters = string.Format(Configuration.ProjectJoiner.MergeParams!, filePath1, filePath2, resultPath);
            Log.Debug("FFMpeg parameters: {ffmpeg} {parameters}", Configuration.ProjectJoiner.WinFFMpeg, parameters);

            Process? ffmpeg = null;
            try
            {
                ffmpeg = Process.Start(new ProcessStartInfo
                {
                    FileName = Configuration.ProjectJoiner.WinFFMpeg,
                    Arguments = parameters,
                    RedirectStandardInput = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                });

                var lastRead = DateTime.Now;
                var outTask = ffmpeg.StandardOutput.ReadToEndAsync();
                var errTask = Task.Run(async () =>
                {
                    char[] bfr = new char[256];
                    StringWriter sw = new StringWriter();
                    int cnt;
                    while ((cnt = await ffmpeg.StandardError.ReadBlockAsync(bfr)) > 0)
                    {
                        lastRead = DateTime.Now;
                        sw.Write(bfr.AsSpan().Slice(0, cnt));
                    }
                    return sw.ToString();
                });

                while (!ffmpeg.HasExited && lastRead + TimeSpan.FromSeconds(10) > DateTime.Now)
                {
                    await Task.Delay(1000);
                    ffmpeg.Refresh();
                }

                if (!ffmpeg.HasExited)
                {
                    throw new Exception("FFmpeg got stuck.");
                }
                else if (ffmpeg.ExitCode != 0)
                {
                    Log.Fatal("FFmpeg failed with error: {error}, output: {out}", await errTask, await outTask);
                    throw new Exception("FFmpeg failed.");
                }
                else
                {
                    return File.OpenRead(resultPath);
                }
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "Error when merging.");
                throw;
            }
            finally
            {
                Log.Information("Cleaning up");
                if (!ffmpeg?.HasExited ?? false)
                    ffmpeg?.Kill();

                File.Delete(filePath1);
                File.Delete(filePath2);
            }
        }
    }
}
