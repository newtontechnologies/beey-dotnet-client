using Polly;
using Polly.Retry;
using System;
using System.Collections.Generic;
using System.Text;

namespace BeeyApi
{
    internal class RetryPolicies
    {
        private const string retryWarningMessage = "Attempt {0} - Request failed with exception '{1}'. Waiting {2}s before retry.";
        private const int networkErrorRetryCount = 3;
        public const string retryErrorMessage = "Error during communication with server.";

        private static TimeSpan CalculateWaitTime(int attempt)
        {
            return TimeSpan.FromSeconds(attempt);
        }

        internal static AsyncRetryPolicy<T> CreateAsyncNetworkPolicy<T>(Func<T> defaultValueCreator, Logging.ILog logger)
        {
            return Policy<T>.Handle<Exception>(IsRetriableException)
                .WaitAndRetryAsync(networkErrorRetryCount,
                    i => CalculateWaitTime(i),
                    (ex, timeSpan, retryCount, context) =>
                    {
                        logger.Log(Logging.LogLevel.Warn, () => string.Format(retryWarningMessage, retryCount, ex.Exception.Message, timeSpan));
                    });
        }

        internal static bool IsRetriableException(Exception ex)
        {
            return !(ex is UnauthorizedAccessException)
                && (ex is System.Net.WebException
                || ex is System.Net.Http.HttpRequestException
                || ex is System.Net.Sockets.SocketException
                || ex is System.Net.WebSockets.WebSocketException
                || (ex is WebSockets.WebSocketClosedException bEx
                    && bEx.CloseStatus != System.Net.WebSockets.WebSocketCloseStatus.PolicyViolation)
                );
        }
    }
}
