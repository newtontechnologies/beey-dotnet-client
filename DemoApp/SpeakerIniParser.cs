using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace DemoApp
{
    static class SpeakerIniParser
    {
        static SpeakerIniParser()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        public static List<(string SectionName, List<string> Keys)> Parse(string file)
        {
            if (!File.Exists(file))
            {
                throw new ArgumentException("File does not exist.", nameof(file));
            }

            var fileContent = File.ReadAllText(file, Encoding.GetEncoding(1250));

            return fileContent.Split("[", StringSplitOptions.RemoveEmptyEntries)
                .Select(section =>
                {
                    var sectionParts = section.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                        .Where(s => s.Trim() != "")
                        .Select((str, i) => (str.Trim(), i));
                    int nameIndex = sectionParts.Single(p => p.Item1.EndsWith("]")).i;

                    if (nameIndex < 0)
                        throw new FormatException("Missing section name.");

                    var sectionName = sectionParts.Single(p => p.i == nameIndex).Item1.TrimEnd(']');
                    var keys = sectionParts.Where(p => p.i != nameIndex).Select(p => p.Item1).ToList();

                    return (SectionName: sectionName, Keys: keys);
                }).ToList();
        }
    }

}
