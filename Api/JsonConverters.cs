using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using TranscriptionCore;

namespace Beey.Api;

class JsonConverters
{
    public static SpeakerJsonConverter Speaker = new SpeakerJsonConverter();
}

class SpeakerJsonConverter : JsonConverter<Speaker>
{
    public override Speaker? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        return new Speaker(System.Xml.Linq.XElement.Parse(reader.GetString()));
    }

    public override void Write(Utf8JsonWriter writer, Speaker value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Serialize().ToString());
    }
}
