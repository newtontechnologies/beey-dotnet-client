using BeeyApi;
using BeeyUI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TranscriptionCore;

namespace DemoApp
{
    public class SpeakerUpdater
    {
        /// <summary>
        /// Returns speakers, that were not inserted, or empty list.
        /// </summary>
        /// <param name="speakers"></param>
        /// <param name="speakerApi"></param>
        /// <returns></returns>
        public static List<Speaker> InsertNewSpeakers(IEnumerable<Speaker> speakers, BeeyClient beey)
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
                }
                catch (Exception)
                {
                    // do nothing
                }

                if (newSpeaker == null)
                {
                    unsuccessfullyInserted.Add(speaker);
                }
            }

            return unsuccessfullyInserted;
        }

        public static IEnumerable<Speaker> GetNewSpeakers(string speakerFile, BeeyClient beey)
        {
            if (beey == null || string.IsNullOrEmpty(speakerFile))
            {
                throw new ArgumentException();
            }

            var speakersFromFile = LoadSpeakers(speakerFile).ToDictionary(s => s.FullName);
            var dbSpeakers = GetAllSpeakers(beey).ToDictionary(s => s.FullName);

            var newSpeakers = speakersFromFile.Keys.Except(dbSpeakers.Keys);

            return newSpeakers.Select(s => speakersFromFile[s]);
        }

        public static IEnumerable<Speaker> GetAllSpeakers(BeeyClient beey)
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

        public static List<Speaker> LoadSpeakers(string file)
        {
            if (File.Exists(file))
            {
                var ini = SpeakerIniParser.Parse(File.ReadAllText(file));

                var speakers = ini.Select((s, si) =>
                {
                    var name = s.SectionName.Split(';');

                    return new Speaker()
                    {
                        Surname = name[0],
                        FirstName = name.Length > 1 ? name[1] : null,
                        Attributes = s.Keys.Select((kd, i) => new SpeakerAttribute(i.ToString(), "role", kd)).ToList()
                    };
                }
                );

                return speakers.ToList();
            }
            else
            {
                throw new Exception("File does not exist.");
            }
        }

        /// <summary>
        /// Simple and dumb equality checking.
        /// </summary>
        /// <param name="speaker1"></param>
        /// <param name="speaker2"></param>
        /// <returns></returns>
        private static bool IsDuplicit(Speaker speaker1, Speaker speaker2)
        {
            bool result = speaker1.FullName == speaker2.FullName;
            result = result
                && speaker1.Attributes.Select(a => a.Value)
                    .Intersect(speaker2.Attributes.Select(a => a.Value))
                    .Any();

            return result;
        }

        public static List<IEnumerable<Speaker>> FindDuplicitSpeakers(IEnumerable<Speaker> speakers)
        {
            var result = new List<IEnumerable<Speaker>>();
            foreach (var speaker in speakers)
            {
                var duplicits = speakers.Where(s => IsDuplicit(speaker, s));
                if (duplicits.Count() > 1)
                {
                    result.Add(duplicits);
                }
            }

            return result;
        }

        /// <summary>
        /// Finds duplicit speakers in DB caused by incorrect import from file.
        /// </summary>
        /// <param name="speakerFile"></param>
        /// <param name="speakerApi"></param>
        /// <returns></returns>
        public static List<IEnumerable<Speaker>> FindDbDuplicitiesFromFile(string speakerFile, BeeyClient beey)
        {
            var speakersFromFile = LoadSpeakers(speakerFile)
                .ToDictionary(s => s.FullName);
            var dbSpeakers = GetAllSpeakers(beey)
                .ToDictionary(s => s.FullName);

            var dbSpeakersFromFile = speakersFromFile.Keys
                .Intersect(dbSpeakers.Keys)
                .Select(fullName => speakersFromFile[fullName]);

            return FindDuplicitSpeakers(dbSpeakersFromFile);
        }
    }
}
