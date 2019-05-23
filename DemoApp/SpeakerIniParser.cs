using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DemoApp
{
    class SpeakerIniParser
    {
        public SpeakerIniParser()
        {

        }

        public SpeakerIniFile Parse(string fileContent)
        {
            var result = new SpeakerIniFile();

            var sections = fileContent.Split("\n\n", StringSplitOptions.RemoveEmptyEntries);
            foreach (var section in sections)
            {
                var sectionParts = section.Split('\n', StringSplitOptions.RemoveEmptyEntries).Select((str,i) => (str,i));
                var speakerSection = new SpeakerSection();

                int nameIndex = sectionParts.Single(p => p.str.StartsWith("[") && p.str.EndsWith("]")).i;

                if (nameIndex < 0)
                {
                    throw new FormatException("Missing section name.");
                }

                speakerSection.SectionName = sectionParts.Single(p => p.i == nameIndex).str.TrimStart('[').TrimEnd(']');
                var keys = sectionParts.Where(p => p.i != nameIndex).Select(p => p.str);

                foreach (var key in keys)
                {
                    speakerSection.Keys.Add(new SpeakerKey() { KeyName = key });
                }

                result.Sections.Add(speakerSection);
            }

            return result;
        }
    }

    class SpeakerIniFile
    {
        public List<SpeakerSection> Sections { get; } = new List<SpeakerSection>();
    }
    class SpeakerSection
    {
        public string SectionName { get; set; }
        public List<SpeakerKey> Keys { get; } = new List<SpeakerKey>();

    }
    class SpeakerKey
    {
        public string KeyName { get; set; }
    }
}
