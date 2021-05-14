using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Beey.Api.Rest
{
    /// <summary>
    /// When used, HttpClient.SendAsync will correctly throw TimeoutException on request timeout.
    /// HttpClient.Timeout must be set to infinity for this handler to work correctly.
    /// </summary>
    internal class TimeoutHandler : DelegatingHandler
    {
        private static readonly TimeSpan defaultTimeout = TimeSpan.FromSeconds(100);
        private readonly TimeSpan timeout;

        /// <summary>
        /// Set HttpClient's timeout to infinity.
        /// </summary>
        /// <param name="timeout"></param>
        public TimeoutHandler() : this(defaultTimeout) { }

        /// <summary>
        /// Set HttpClient's timeout to infinity.
        /// </summary>
        /// <param name="timeout"></param>
        public TimeoutHandler(TimeSpan timeout) : base(new HttpClientHandler())
        {
            this.timeout = timeout;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            try
            {
                var cts = timeout == Timeout.InfiniteTimeSpan
                    ? null
                    : CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                using (cts)
                {
                    cts?.CancelAfter(timeout);
                    return await base.SendAsync(request, cts?.Token ?? cancellationToken);
                }
            }
            catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                throw new TimeoutException($"Request timeouted.");
            }
            catch (Exception ex)
            {
                string de = ex.Message;
                throw;
            }
        }
    }
}
