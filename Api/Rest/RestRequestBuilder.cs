﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Beey.Api.Rest;

public class RestRequestBuilder
{
    public static HttpClient httpClient = new HttpClient(new TimeoutHandler()) { Timeout = Timeout.InfiniteTimeSpan };

    private readonly ILogger<RestRequestBuilder> logger = LoggerFactoryProvider.LoggerFactory.CreateLogger<RestRequestBuilder>();

    private Request request;

    public RestRequestBuilder(string url)
    {
        request = new Request(url);
    }

    public RestRequestBuilder EndPoint(string endPoint)
    {
        request.EndPoint = "/" + endPoint.TrimStart('/').TrimEnd('/');

        return this;
    }

    public RestRequestBuilder AddUrlSegment(string? segment)
    {
        if (!string.IsNullOrWhiteSpace(segment))
        {
            request.EndPoint += "/" + segment.TrimStart('/').TrimEnd('/');
        }
        return this;
    }

    public RestRequestBuilder AddHeader(string name, string? value)
    {
        if (value != null)
        {
            request.Headers.Add(name, value);
        }
        return this;
    }

    public RestRequestBuilder AddHeaders((string name, string value)[] headers)
    {
        foreach (var p in headers)
            AddHeader(p.name, p.value);

        return this;
    }

    public RestRequestBuilder AddParameter(string name, string? value)
    {
        if (value != null)
        {
            request.Parameters.Add(name, value);
        }
        return this;
    }

    public RestRequestBuilder AddParameter(string name, object? value) => AddParameter(name, value?.ToString());

    public RestRequestBuilder AddParameters(params (string name, string? value)[] pars)
    {
        foreach (var p in pars)
            AddParameter(p.name, p.value);

        return this;
    }

    public RestRequestBuilder AddParameters(params (string name, object? value)[] pars)
    {
        foreach (var p in pars)
            AddParameter(p.name, p.value);

        return this;
    }

    public RestRequestBuilder SetContent(HttpContent content)
    {
        request.Content = content;
        return this;
    }

    public RestRequestBuilder SetBody(string body, string contentType = "application/json")
    {
        request.Body = body;
        request.BodyContentType = contentType;
        return this;
    }

    public RestRequestBuilder AddFile(string name, byte[] file)
    {
        if (!request.FileBytes.ContainsKey(name))
        {
            request.FileBytes.Add(name, file);
        }
        else
        {
            request.FileBytes[name] = file;
        }

        return this;
    }

    public RestRequestBuilder AddFile(string name, Stream stream)
    {
        if (!request.FileStreams.ContainsKey(name))
        {
            request.FileStreams.Add(name, stream);
        }
        else
        {
            request.FileStreams[name] = stream;
        }

        return this;
    }

    public async Task<Response> ExecuteAsync(HttpMethod method, CancellationToken cancellationToken)
    {
        var policy = RetryPolicies.CreateAsyncNetworkPolicy<Response>(logger);
        var result = await policy.ExecuteAsync(
            async (c) =>
            {
                var requestMessage = CreateHttpRequest(this.request, method);
                var responseMessage = await httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, c);
                var content = await responseMessage.Content.ReadAsStreamAsync();
                return new Response(responseMessage, content);
            },
            cancellationToken);
        return result;
    }

    private static HttpRequestMessage CreateHttpRequest(Request request, HttpMethod method)
    {
        var uri = new UriBuilder(request.Url + request.EndPoint ?? "");

        var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
        foreach (var parameter in request.Parameters)
        {
            query.Add(parameter.Key, parameter.Value);
        }
        uri.Query = query.ToString();

        var result = new HttpRequestMessage(
            new System.Net.Http.HttpMethod(method.ToString()), uri.Uri);

        foreach (var header in request.Headers)
        {
            result.Headers.Add(header.Key, header.Value);
        }


        if (request.Content is { })
        {
            result.Content = request.Content;
        }
        else if (!string.IsNullOrWhiteSpace(request.Body))
        {
            result.Content = new StringContent(request.Body, Encoding.UTF8, request.BodyContentType);
        }
        else
        {
            var content = new MultipartFormDataContent();
            foreach (var file in request.FileBytes)
            {
                content.Add(new ByteArrayContent(file.Value), "files", file.Key);
            }

            foreach (var file in request.FileStreams)
            {
                content.Add(new StreamContent(file.Value), "files", file.Key);
            }

            if (content.GetEnumerator().MoveNext())
            {
                result.Content = content;
            }
        }

        return result;
    }

    class Request
    {
        public string Url { get; set; }
        public string? EndPoint { get; set; }
        public Dictionary<string, string> Parameters { get; } = new Dictionary<string, string>();
        public Dictionary<string, string> Headers { get; } = new Dictionary<string, string>();
        public Dictionary<string, System.IO.Stream> FileStreams { get; } = new Dictionary<string, System.IO.Stream>();
        public Dictionary<string, byte[]> FileBytes { get; } = new Dictionary<string, byte[]>();

        public HttpContent? Content { get; set; }
        public string? Body { get; set; }
        public string? BodyContentType { get; set; }

        public Request(string url)
        {
            Url = url.TrimStart('/').TrimEnd('/');
        }
    }
}

public enum HttpMethod { GET, POST, PUT, DELETE };

public class Response
{
    public HttpStatusCode StatusCode { get; private set; }
    public bool IsSuccessStatusCode { get; private set; }
    public string ErrorMessage { get; private set; }
    public HttpResponseMessage HttpResponseMessage { get; }
    public Stream Content { get; private set; }

    public Response(HttpResponseMessage httpResponseMessage, Stream content)
    {
        StatusCode = httpResponseMessage.StatusCode;
        IsSuccessStatusCode = httpResponseMessage.IsSuccessStatusCode;
        ErrorMessage = httpResponseMessage.ReasonPhrase;
        HttpResponseMessage = httpResponseMessage;
        Content = content;
    }

    public string GetStringContent()
    {
        string result;
        using (var streamReader = new StreamReader(Content))
        {
            result = streamReader.ReadToEnd();
        }

        return result;
    }
}
