using Beey.Client;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TranscriptionCore;

namespace SpeakerDbUpdater
{
    class Program
    {
        static async Task<int> Main(string[] args)
        {
            Configuration.Load(args);
            if (Configuration.SpeakerDbUpdater.Url == null
                || Configuration.SpeakerDbUpdater.Login == null
                || Configuration.SpeakerDbUpdater.Password == null
                || (Configuration.SpeakerDbUpdater.UpdateDb && Configuration.SpeakerDbUpdater.IniPath == null))
            {
                Log.Fatal("Missing settings: Url, Login, Password or IniFile");
                return -1;
            }

            var now = DateTime.Now.ToString("yyyy-MM-dd_hh-mm-ss");
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.File($"{now}_beey.log", Serilog.Events.LogEventLevel.Information)
                .WriteTo.File($"{now}_beey_verbose.log")
                .CreateLogger();

            var beey = new BeeyClient(Configuration.SpeakerDbUpdater.Url);
            await beey.LoginAsync(Configuration.SpeakerDbUpdater.Login, Configuration.SpeakerDbUpdater.Password);

            if (Configuration.SpeakerDbUpdater.UpdateDb)
            {
                UpdateDbFromFile(Configuration.SpeakerDbUpdater.IniPath!, Configuration.SpeakerDbUpdater.InsertOnlyNew, beey);
            }

            Thread.Sleep(2000);

            if (Configuration.SpeakerDbUpdater.RemoveDuplicities)
            {
                RemoveDuplicities(beey);
            }

            return 0;
        }

        private static void RemoveDuplicities(BeeyClient beey)
        {
            Log.Information("Loading Speakers from DB.");
            var dbSpeakers = GetSpeakers(beey);
            Log.Information("Found {count} Speakers.", dbSpeakers.Count);

            Log.Information("Searching for duplicit Speakers in DB.");
            var dbDuplicities = FindDuplicities(dbSpeakers);
            Log.Information("Found {count} Speakers with duplicities.", dbDuplicities.Count);

            if (dbDuplicities.Any())
            {
                Log.Information("Removing duplicit Speakers.");
                var removed = RemoveDuplicitiesFromDb(dbDuplicities, beey);

                if (removed.Any())
                {
                    int allDuplicities = 0;
                    int removedDuplicities = 0;
                    foreach (var speaker in removed)
                    {
                        if (speaker.Value.Success.Any() || speaker.Value.Fail.Any())
                        {
                            allDuplicities += speaker.Value.Success.Count + speaker.Value.Fail.Count;
                            removedDuplicities += speaker.Value.Success.Count;
                        }
                    }
                    if (removedDuplicities > 0)
                        Log.Information("Successfuly removed {count} duplicitites.", removedDuplicities);
                    if (allDuplicities - removedDuplicities > 0)
                        Log.Error("Failed to remove {count} duplicities.", allDuplicities - removedDuplicities);
                }
            }
            Log.Information("Process finished.");
        }

        private static void UpdateDbFromFile(string speakerFile, bool insertOnlyNew, BeeyClient beey)
        {
            Log.Information("Loading Speakers from {file}.", Path.GetFileName(speakerFile));
            var fileSpeakers = GetSpeakers(speakerFile);
            Log.Information("Found {count} Speakers.", fileSpeakers.Count);

            if (fileSpeakers.Any())
            {
                Log.Information("Loading Speakers from DB.");
                var dbSpeakers = GetSpeakers(beey);
                Log.Information("Found {count} Speakers.", dbSpeakers.Count);
                List<Speaker> newSpeakers;

                if (insertOnlyNew)
                {
                    var fileSpeakersCount = fileSpeakers.Count();
                    fileSpeakers = fileSpeakers.Distinct((CustomEqualityComparer<Speaker>)IsDuplicit).ToList();
                    if (fileSpeakersCount != fileSpeakers.Count)
                        Log.Warning("Only {count} out of {originalCount} distinct Speakers in {file}.", fileSpeakers.Count, fileSpeakersCount, speakerFile);

                    List<Speaker> notNewSpeakers;
                    (newSpeakers, notNewSpeakers) = SelectNewSpeakers(fileSpeakers, dbSpeakers);
                    if (notNewSpeakers.Any())
                        Log.Verbose($"Speakers already in DB: {notNewSpeakers.Skip(1).Aggregate(notNewSpeakers.First().FullName, (s, speaker) => $"{s}, {speaker.FullName}")}");

                    Log.Information($"{(newSpeakers.Any() ? "Inserting" : "Found")} {{count}} new Speakers.", newSpeakers.Count);
                }
                else
                {
                    newSpeakers = fileSpeakers;
                    Log.Information("Inserting Speakers.");
                }
                var notInserted = InsertSpeakersToDb(newSpeakers, beey);

                if (newSpeakers.Any())
                    Log.Information("Successfuly inserted {count} new Speakers.", newSpeakers.Count - notInserted.Count);

                if (notInserted.Any())
                    Log.Warning("{count} Speakers failed to be inserted.", notInserted.Count);
            }
            Log.Information("Process finished.");
        }

        static (int totalCount, List<Speaker> failed) UpdateDb(List<Speaker> newSpeakers, BeeyClient beey)
        {
            var notInserted = InsertSpeakersToDb(newSpeakers, beey);

            return (newSpeakers.Count(), notInserted);
        }

        static Dictionary<string, (string FullName, List<string> Success, List<string> Fail)> RemoveDuplicitiesFromDb(List<(Speaker speaker, IEnumerable<Speaker> duplicities)> dbDuplicities, BeeyClient beey)
        {
            var removedSpeakersIds = new Dictionary<string, (string FullName, List<string> Success, List<string> Fail)>();
            foreach (var duplicity in dbDuplicities)
            {
                Log.Information("Speaker {name} ({dbId})", duplicity.speaker.FullName, duplicity.speaker.DBID);
                (string FullName, List<string> Success, List<string> Fail) removed
                    = (duplicity.speaker.FullName, new List<string>(), new List<string>());
                foreach (var speaker in duplicity.duplicities)
                {
                    if (beey.DeleteSpeakerAsync(speaker.DBID).TryAsync().Result)
                    {
                        removed.Success.Add(speaker.DBID);
                    }
                    else
                    {
                        removed.Fail.Add(speaker.DBID);
                    }
                }
                removedSpeakersIds.Add(duplicity.speaker.DBID, removed);

                if (removed.Success.Any())
                    Log.Information("\tRemoved {count} - {aggregate}.", removed.Success.Count, removed.Success.Aggregate((s1, s2) => $"{s1}, {s2}"));
                if (removed.Fail.Any())
                    Log.Error("\tFailed to remove {count} - {aggregate}.", removed.Fail.Count, removed.Fail.Aggregate((s1, s2) => $"{s1}, {s2}"));
            }

            return removedSpeakersIds;
        }

        /// <summary>
        /// Returns speakers, that were not inserted, or empty list.
        /// </summary>
        /// <param name="speakers"></param>
        /// <param name="speakerApi"></param>
        /// <returns></returns>
        static List<Speaker> InsertSpeakersToDb(IEnumerable<Speaker> speakers, BeeyClient beey)
        {
            if (beey == null || speakers == null)
            {
                throw new ArgumentException();
            }

            var unsuccessfullyInserted = new List<Speaker>();
            foreach (var speaker in speakers)
            {
                Speaker? newSpeaker = null;
                try
                {
                    newSpeaker = beey.CreateSpeakerAsync(speaker).Result;
                    Log.Verbose("{speaker} inserted.", newSpeaker.FullName);
                }
                catch (Exception)
                {
                    Log.Error("{speaker} could not be inserted.", speaker.FullName);
                }

                if (newSpeaker == null)
                {
                    unsuccessfullyInserted.Add(speaker);
                }
            }

            return unsuccessfullyInserted;
        }

        static (List<Speaker> newSpeakers, List<Speaker> notNewSpeakers) SelectNewSpeakers(List<Speaker> speakersToAdd, List<Speaker> currentSpeakers)
        {
            var newSpeakers = speakersToAdd.Except(currentSpeakers, (CustomEqualityComparer<Speaker>)IsDuplicit);
            var notNewSpeakers = speakersToAdd.Except(newSpeakers);

            return (newSpeakers.ToList(), notNewSpeakers.ToList());
        }

        static List<Speaker> GetSpeakers(BeeyClient beey)
        {
            var result = new List<Speaker>();
            int count = 1000;
            int skip = 0;

            var speakers = beey.ListSpeakersAsync(count, skip).Result;
            while (speakers != null && speakers.ListedCount > 0)
            {
                result.AddRange(speakers.List);
                skip += count;
                speakers = beey.ListSpeakersAsync(count, skip).Result;
            }

            return result;
        }

        static List<Speaker> GetSpeakers(string file)
        {
            var ini = SpeakerIniParser.Parse(file);

            var speakers = ini.Select((s, si) =>
            {
                var name = s.SectionName.Split(';');

                return new Speaker()
                {
                    Surname = name[0],
                    FirstName = name.Length > 1 ? name[1] : null,
                    Attributes = s.Keys.Select((kd, i) => new SpeakerAttribute(i.ToString(), "role", kd)).ToList()
                };
            });

            return speakers.ToList();
        }

        /// <summary>
        /// Simple and dumb equality checking.
        /// </summary>
        /// <param name="speaker1"></param>
        /// <param name="speaker2"></param>
        /// <returns></returns>
        private static bool IsDuplicit(Speaker speaker1, Speaker speaker2)
        {
            bool result = speaker1.FullName == speaker2.FullName
                && (!speaker1.Attributes.Any()
                    || !speaker2.Attributes.Any()
                    || speaker1.Attributes
                        .Select(a => a.Value)
                        .Intersect(speaker2.Attributes.Select(a => a.Value))
                        .Any()
                    );

            return result;

            
        }

        /// <summary>
        /// Finds speakers with duplicitites only among suspected speakers.
        /// </summary>
        /// <param name="speakers"></param>
        /// <returns></returns>
        static List<(Speaker, IEnumerable<Speaker>)> FindDuplicities(List<Speaker> speakers)
        {
            var uniqueSpeakers = speakers.Distinct((CustomEqualityComparer<Speaker>)IsDuplicit);
            var speakersWithoutUnique = speakers.Except(uniqueSpeakers).ToList();
            var result = new List<(Speaker, IEnumerable<Speaker>)>();
            foreach (var speaker in uniqueSpeakers)
            {
                var duplicits = speakersWithoutUnique.Where(s => IsDuplicit(speaker, s));
                if (duplicits.Any())
                {
                    result.Add((speaker, duplicits));
                }
            }

            return result;
        }
    }
}
