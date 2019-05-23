using BeeyApi.POCO;
using Newtonsoft.Json;
using Polly;
using Polly.Retry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BeeyApi.Rest
{
    public abstract class BaseApi<TApi> where TApi : BaseApi<TApi>
    {
        public static Error NoError { get; } = new Error("OK", true);

        internal Logging.ILog Logger = Logging.LogProvider.For<TApi>();
        protected string Url { get; set; }
        protected string? EndPoint { get; set; }

        public Error LastError { get; protected set; }
        public HttpStatusCode LastHttpStatusCode { get; protected set; }

        protected void SetLastError(string content, HttpStatusCode statusCode)
        {
            try
            {
                LastError = JsonConvert.DeserializeObject<Error>(content);
            }
            catch (Exception)
            {
                LastError = new Error($"Unknown error:{Environment.NewLine}{content}");
            }

            LastHttpStatusCode = statusCode;
        }

        public BaseApi(string url)
        {
            Url = url;
            LastError = NoError;
        }

        internal virtual RestRequestBuilder CreateBuilder()
        {
            return new RestRequestBuilder(this.Url).AddUrlSegment(EndPoint);
        }

        internal T HandleResponse<T>(Response response, HttpStatusCode successStatusCode, Func<Response, T> success, Func<Response, T> fail)
        {
            if (response.StatusCode != successStatusCode)
            {
                SetLastError(response.GetStringContent(), response.StatusCode);

                if (response.StatusCode == HttpStatusCode.InternalServerError)
                {
                    Logger.Log(Logging.LogLevel.Error, () => $"Server error: {LastError.Message}");
                    throw new ApplicationException(LastError.Message);
                }
                return fail(response);
            }

            LastError = NoError;
            return success(response);
        }

        internal async Task<T> HandleResponseAsync<T>(Response response, HttpStatusCode successStatusCode,
            Func<Response, CancellationToken, Task<T>> success, Func<Response, CancellationToken, Task<T>> fail, CancellationToken cancellationToken)
        {
            if (response.StatusCode != successStatusCode)
            {
                SetLastError(response.GetStringContent(), response.StatusCode);

                if (response.StatusCode == HttpStatusCode.InternalServerError)
                {
                    Logger.Log(Logging.LogLevel.Error, () => LastError.Message);
                    throw new ApplicationException(LastError.Message);
                }
                return await fail(response, cancellationToken);
            }

            LastError = NoError;
            return await success(response, cancellationToken);

        }
    }
}
