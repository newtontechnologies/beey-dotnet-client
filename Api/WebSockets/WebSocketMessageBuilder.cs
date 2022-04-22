using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;

namespace Beey.Api.WebSockets;

internal class WebSocketMessageBuilder
{
    private static readonly Logging.ILog logger = Logging.LogProvider.For<WebSocketMessageBuilder>();

    internal const int bufferSize = 1024 * 4;
    private WebSocketMessage webSocketMessage;

    public WebSocketMessageBuilder(string url)
    {
        webSocketMessage = new WebSocketMessage(url);
    }

    public WebSocketMessageBuilder Url(string url)
    {
        string tmpUrl = url.TrimStart('/').TrimEnd('/');
        if (tmpUrl.StartsWith("http"))
        {
            tmpUrl = "ws" + tmpUrl.Substring(4);
        }
        webSocketMessage.Url = tmpUrl;

        return this;
    }

    public WebSocketMessageBuilder AddUrlSegment(string? segment)
    {
        if (!string.IsNullOrWhiteSpace(segment))
        {
            webSocketMessage.EndPoint += "/" + segment.TrimStart('/').TrimEnd('/');
        }
        return this;
    }

    public WebSocketMessageBuilder AddParameter(string name, string value)
    {
        if (value != null)
        {
            webSocketMessage.Parameters.Add(name, value);
        }
        return this;
    }

    public WebSocketMessageBuilder AddParameter(string name, object value) => AddParameter(name, value.ToString());

    public WebSocketMessageBuilder AddParameters(params (string name, string value)[] pars)
    {
        foreach (var p in pars)
            AddParameter(p.name, p.value);

        return this;
    }

    public async Task<ClientWebSocket> OpenConnectionAsync(CancellationToken cancellationToken)
    {
        var ws = new ClientWebSocket();
        var uri = CreateUri(webSocketMessage);
        await ws.ConnectAsync(uri, cancellationToken);

        return ws;
    }

    private Uri CreateUri(WebSocketMessage webSocketMessage)
    {
        var uri = new UriBuilder(webSocketMessage.Url + webSocketMessage.EndPoint ?? "");
        var query = System.Web.HttpUtility.ParseQueryString("");
        foreach (var parameter in webSocketMessage.Parameters)
        {
            query.Add(parameter.Key, parameter.Value);
        }
        uri.Query = query.ToString();
        return uri.Uri;
    }

    class WebSocketMessage
    {
        public string Url { get; set; }
        public string? EndPoint { get; set; }
        public Dictionary<string, string> Parameters { get; } = new Dictionary<string, string>();

        public WebSocketMessage(string url)
        {
            Url = url.TrimStart('/').TrimEnd('/');
            if (Url.StartsWith("http"))
            {
                Url = "ws" + Url.Substring(4);
            }
        }
    }
}
