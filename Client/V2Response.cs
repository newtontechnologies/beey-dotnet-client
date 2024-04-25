using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Beey.Client;


internal class V2Response
{
    public static T? Deserialize<T>(string jsonResponse)
    {
        var jNode = JsonNode.Parse(jsonResponse, new JsonNodeOptions() { PropertyNameCaseInsensitive = true });

        if (jNode is JsonObject jObject && jObject.TryGetPropertyValue("Data", out var jData))
        {
            return jData != null
                ? jData.Deserialize<T>()
                : default;
        }
        else
        {
            return jNode.Deserialize<T>();
        }
    }
}