using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace SpeakerDbUpdater

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

            string encoding;
            using (var reader = File.OpenRead(file))
            {
                encoding = DetectFileEncoding(reader);
            }

            var fileContent = File.ReadAllText(file, Encoding.GetEncoding(encoding));

            return fileContent.Split("[", StringSplitOptions.RemoveEmptyEntries)
                .Where(s => s.Trim() != "")
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

        // https://stackoverflow.com/a/52227667
        private static string DetectFileEncoding(Stream fileStream)
        {
            var Utf8EncodingVerifier = Encoding.GetEncoding("utf-8", new EncoderExceptionFallback(), new DecoderExceptionFallback());
            using (var reader = new StreamReader(fileStream, Utf8EncodingVerifier,
                   detectEncodingFromByteOrderMarks: true, leaveOpen: true, bufferSize: 1024))
            {
                string detectedEncoding;
                try
                {
                    while (!reader.EndOfStream)
                    {
                        var line = reader.ReadLine();
                    }
                    detectedEncoding = reader.CurrentEncoding.BodyName;
                }
                catch (Exception)
                {
                    // Failed to decode the file using the BOM/UTF8. 
                    // Assume it's local ANSI
                    detectedEncoding = "ISO-8859-1";
                }
                // Rewind the stream
                fileStream.Seek(0, SeekOrigin.Begin);
                return detectedEncoding;
            }
        }
    }

}
