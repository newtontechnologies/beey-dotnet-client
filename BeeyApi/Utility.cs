using BeeyApi.WebSockets;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Net;
using System.Text;

namespace BeeyApi
{
    internal class Utility
    {
        public static Dictionary<string, string> AnonymousObjectToDictionary(object values)
        {
            var result = new Dictionary<string, string>();
            if (values is IDictionary<string, object> dict)
            {
                foreach (var value in dict)
                {
                    result.Add(value.Key, value.Value?.ToString());
                }
            }
            else if (values != null)
            {
                foreach (PropertyDescriptor property in TypeDescriptor.GetProperties(values))
                {
                    result.Add(property.Name, property.GetValue(values)?.ToString());
                }
            }

            return result;
        }

        internal static void LogApiException(Exception ex, Logging.ILog logger)
        {
            if (ex is WebSocketClosedException wsEx && wsEx.CloseStatus.HasValue)
            {
                logger.Log(Logging.LogLevel.Error, () => $"Error in Beey API, WebSocket closed ({wsEx.CloseStatus?.ToString()}) with message '{wsEx.Message}'.", wsEx);
            }
            else
            {
                logger.Log(Logging.LogLevel.Error, () => $"Error in Beey API: '{ex.Message}'", ex);
            }           
        }
    }
}
