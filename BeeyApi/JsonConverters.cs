using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using TranscriptionCore;

namespace BeeyApi
{
    class JsonConverters
    {
        public static SpeakerJsonConverter Speaker = new SpeakerJsonConverter();
    }

    class SpeakerJsonConverter : JsonConverter<Speaker>
    {
        public override Speaker ReadJson(JsonReader reader, Type objectType, Speaker existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            return new Speaker(System.Xml.Linq.XElement.Parse((string)reader.Value));
        }

        public override void WriteJson(JsonWriter writer, Speaker value, JsonSerializer serializer)
        {
            writer.WriteValue(value.Serialize());
        }
    }
}
