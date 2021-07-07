using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Beey.Api.DTO
{
    public class ExportFile
    {
        public ExportFile(string fileName, string mimeType, Stream content)
        {
            FileName = fileName;
            MimeType = mimeType;
            Content = content;
        }

        public string FileName { get; }
        public string MimeType { get; }
        public Stream Content { get; }
    }
}
