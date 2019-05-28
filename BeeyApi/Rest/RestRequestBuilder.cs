using Polly;
using Polly.Wrap;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BeeyApi.Rest
{
    internal class RestRequestBuilder
    {
        private static readonly HttpClient httpClient = new HttpClient();
        private static readonly Logging.ILog logger = Logging.LogProvider.For<RestRequestBuilder>();

        private Request request;
        public RestRequestBuilder(string url)
        {
            request = new Request(url);
        }

        public RestRequestBuilder Url(string url)
        {
            request.Url = url.TrimStart('/').TrimEnd('/');

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
        public RestRequestBuilder AddHeaders(object values)
        {
            foreach (var value in Utility.AnonymousObjectToDictionary(values))
            {
                if (value.Value != null)
                {
                    request.Headers.Add(value.Key, value.Value);
                }
            }
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
        public RestRequestBuilder AddParameters(object values)
        {
            foreach (var value in Utility.AnonymousObjectToDictionary(values))
            {
                if (value.Value != null)
                {
                    request.Parameters.Add(value.Key, value.Value);
                }
            }
            return this;
        }

        public RestRequestBuilder SetBody(string body, string format = "text/xml")
        {
            request.Body = body;
            request.BodyFormat = format;
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
                    var responseMessage =  await httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseHeadersRead, c);
                    var content = await responseMessage.Content.ReadAsStreamAsync();
                    return new Response(responseMessage.StatusCode, responseMessage.IsSuccessStatusCode, responseMessage.ReasonPhrase, content);
                },
                cancellationToken);
            return result;
        }

        private static HttpRequestMessage CreateHttpRequest(Request request, HttpMethod method)
        {
            var uri = new UriBuilder(request.Url + request.EndPoint ?? "");

            var query = System.Web.HttpUtility.ParseQueryString("");
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

            if (!string.IsNullOrWhiteSpace(request.Body))
            {
                result.Content = new StringContent(request.Body, Encoding.UTF8, "application/json");
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
            public string? Body { get; set; }
            public string? BodyFormat { get; set; }

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
        public Stream Content { get; private set; }

        public Response(HttpStatusCode statusCode, bool isSuccessStatusCode, string errorMessage, Stream content)
        {
            StatusCode = statusCode;
            IsSuccessStatusCode = isSuccessStatusCode;
            ErrorMessage = errorMessage;
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
}
