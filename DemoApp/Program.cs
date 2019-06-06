using BeeyApi;
using BeeyUI;
using Serilog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TranscriptionCore;

namespace DemoApp
{
    class Program
    {
        static async Task Main(string[] args)
        {
            //Test();
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.File("beey.log")
                .CreateLogger();

            string url = "http://localhost:61497";
            bool bResult = true;

            var beey = new Beey(url);
            bResult = await beey.LoginAsync("milos.kudelka@newtontech.cz", "OVPgod").TryAsync();

            //string speakerFile = @"..\..\..\tvrlidi.ini";
            //SpeakerUpdater.LoadSpeakers(speakerFile);
            //UpdateDatabase(speakerFile, beey);
            //var removed = RemoveDbDuplicitiesFromFile(speakerFile, beey);

            var speakers = await beey.ListSpeakersAsync(100).TryAsync();
            var speaker = await beey.GetSpeakerAsync(speakers.Value?.List.FirstOrDefault()?.DBID ?? "");

            var projects = await beey.ListProjectsAsync(100);
            var project = await beey.GetProjectAsync(projects?.List.FirstOrDefault()?.Id ?? -1);

            var mp3Path = @"c:\Users\milos.kudelka\Downloads\test01.mp3";
            if (project != null)
            {
                bResult = await beey.UploadFileAsync(project.Id, new FileInfo(mp3Path), "cz", false);
            }
        }

        private static void Test()
        {
            int times = 10000;
            var file = new FileInfo(@"c:\Users\milos.kudelka\Downloads\test01.mp3");

            var watch = new Stopwatch();
            watch.Start();
            for (int x = 0; x < times; x++)
            {
                List<byte> result = new List<byte>();
                using (var s = file.Open(FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    byte[] buffer = new byte[1024 * 4];
                    using (MemoryStream ms = new MemoryStream(buffer))
                    using (BinaryWriter bw = new BinaryWriter(ms))
                    {
                        while (true)
                        {
                            ms.Seek(0, SeekOrigin.Begin);
                            bw.Write((double)s.Position);

                            var read = s.Read(buffer, sizeof(double) + sizeof(short), buffer.Length - sizeof(double) - sizeof(short));
                            if (read <= 0) //EOF
                                break;


                            ms.Seek(sizeof(double), SeekOrigin.Begin);
                            bw.Write((short)read);
                            var segment = new ArraySegment<byte>(buffer, 0, sizeof(double) + sizeof(short) + read);
                            result.AddRange(segment.ToArray());
                        }
                    }
                }
            }
            watch.Stop();
            var elapsed = watch.Elapsed;

            watch.Restart();
            for (int x = 0; x < times; x++)
            {
                List<byte> result2 = new List<byte>();
                using (var s = file.Open(FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    short sizeOfDouble = sizeof(double);
                    byte[] buffer = new byte[1024 * 4];
                    while (true)
                    {
                        byte[] pos = BitConverter.GetBytes((double)s.Position);
                        for (int i = 0; i < pos.Length; i++)
                        {
                            buffer[i] = pos[i];
                        }

                        var read = s.Read(buffer, sizeof(double) + sizeof(short), buffer.Length - sizeof(double) - sizeof(short));
                        if (read <= 0) //EOF
                            break;

                        byte[] length = BitConverter.GetBytes((short)read);
                        for (int i = 0; i < length.Length; i++)
                        {
                            buffer[i + sizeOfDouble] = length[i];
                        }

                        var segment = new ArraySegment<byte>(buffer, 0, sizeof(double) + sizeof(short) + read);
                        result2.AddRange(segment.ToArray());
                    }
                }
            }
            watch.Stop();
            var elapsed2 = watch.Elapsed;

            double difference = elapsed.TotalSeconds - elapsed2.TotalSeconds;
        }

        static void UpdateDatabase(string speakerFile, Beey beey)
        {
            var newSpeakers = SpeakerUpdater.GetNewSpeakers(speakerFile, beey);
            var notInserted = SpeakerUpdater.InsertNewSpeakers(newSpeakers, beey);
        }

        static Dictionary<string, (List<string> Success, List<string> Fail)> RemoveDbDuplicitiesFromFile(string speakerFile, Beey beey)
        {
            var dbDuplicitiesFromFile = SpeakerUpdater.FindDbDuplicitiesFromFile(speakerFile, beey);

            var removedSpeakersIds = new Dictionary<string, (List<string> Success, List<string> Fail)>();
            foreach (var duplicity in dbDuplicitiesFromFile)
            {
                var firstSpeaker = duplicity.First();
                var duplicitSpeakersToDelete = duplicity.Skip(1);
                foreach (var speaker in duplicitSpeakersToDelete)
                {
                    removedSpeakersIds.Add(firstSpeaker.DBID, (new List<string>(), new List<string>()));

                    if (beey.DeleteSpeakerAsync(firstSpeaker.DBID).Result)
                    {
                        removedSpeakersIds[firstSpeaker.DBID].Success.Add(speaker.DBID);
                    }
                    else
                    {
                        removedSpeakersIds[speaker.DBID].Fail.Add(speaker.DBID);
                    }
                }
            }

            return removedSpeakersIds;
        }
    }
}
