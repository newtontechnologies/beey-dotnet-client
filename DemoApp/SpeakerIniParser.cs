using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DemoApp
{
    static class SpeakerIniParser
    {
        public static List<(string SectionName, List<string> Keys)> Parse(string fileContent)
        {
            return fileContent.Split("\n\n", StringSplitOptions.RemoveEmptyEntries)
                .Select(section =>
                {
                    var sectionParts = section.Split('\n', StringSplitOptions.RemoveEmptyEntries).Select((str, i) => (str, i));
                    int nameIndex = sectionParts.Single(p => p.str.StartsWith("[") && p.str.EndsWith("]")).i;

                    if (nameIndex < 0)
                        throw new FormatException("Missing section name.");

                    var sectionName = sectionParts.Single(p => p.i == nameIndex).str.TrimStart('[').TrimEnd(']');
                    var keys = sectionParts.Where(p => p.i != nameIndex).Select(p => p.str).ToList();

                    return (SectionName: sectionName, Keys: keys);
                }).ToList();
        }
    }

}
