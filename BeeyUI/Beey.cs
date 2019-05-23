using BeeyApi.POCO.Auth;
using BeeyApi;
using Polly;
using Polly.Retry;
using Polly.Wrap;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using BeeyApi.Rest;
using BeeyApi.WebSockets;

namespace BeeyUI
{
    public partial class Beey
    {
        private static readonly Logging.ILog logger = Logging.LogProvider.For<Beey>();

        private string? userEmail;
        // TODO get rid of password in plaintext and still be able to re-login?
        private string? userPassword;

        protected LoginToken? LoginToken { get; set; }
        protected LoginApi LoginApi { get; set; }
        protected SpeakerApi SpeakerApi { get; set; }
        protected ProjectApi ProjectApi { get; set; }
        protected FilesApi FilesApi { get; set; }
        protected WebSocketsApi WebSocketsApi { get; set; }

        public Beey(string url)
        {
            LoginApi = new LoginApi(url);
            SpeakerApi = new SpeakerApi(url);
            ProjectApi = new ProjectApi(url);
            FilesApi = new FilesApi(url);

            string webSocketsUrl = url;
            if (webSocketsUrl.StartsWith("http"))
            {
                webSocketsUrl = "ws" + webSocketsUrl.Substring(4);
            }
            WebSocketsApi = new WebSocketsApi(webSocketsUrl);
        }

        public async Task<string?> LoginAsync(string email, string password,
            CancellationToken cancellationToken = default)
        {
            this.userEmail = email;
            this.userPassword = password;
            LoginToken = await LoginApi.LoginAsync(this.userEmail, this.userPassword, cancellationToken);

            SpeakerApi.Token = LoginToken;
            ProjectApi.Token = LoginToken;
            FilesApi.Token = LoginToken;
            WebSocketsApi.Token = LoginToken;

            if (LoginToken == null)
            {
                return LoginApi.LastError.Message;
            }

            return null;
        }

        public async Task LogoutAsync(CancellationToken cancellationToken = default)
        {
            if (LoginToken != null)
            {
                await LoginApi.LogoutAsync(LoginToken, cancellationToken);
            }

            SpeakerApi.Token = null;
            ProjectApi.Token = null;
            FilesApi.Token = null;
            WebSocketsApi.Token = null;

            LoginToken = null;
            userEmail = null;
            userPassword = null;
        }

        private void RequireAuthorization()
        {
            if (LoginToken == null || userEmail == null || userPassword == null)
            {
                logger.Log(Logging.LogLevel.Error, () => unauthorizedErrorMessage);
                throw new UnauthorizedAccessException();
            }
        }

        private async Task<bool> TryReloginAsync(CancellationToken cancellationToken = default)
        {
            if (this.userEmail == null || this.userPassword == null)
            {
                return false;
            }

            try
            {
                string? result = await LoginAsync(this.userEmail, this.userPassword, cancellationToken);
                return result == null;
            }
            catch (Exception)
            {
                return false;
            }
        }

        #region Polly retry policies

        private const int unauthorizedRetryLoginCount = 1;
        private const string retryUnauthorizedErrorMessage = "Attempt {0} - Request was not authorized. Trying to login and waiting {1}s before retry.";
        private const string unauthorizedErrorMessage = "Request was not authorized.";
        private Context CreatePollyContext(CancellationToken cancellationToken = default)
        {
            var result = new Context
            {
                { "cancellationToken", cancellationToken }
            };
            return result;
        }

        private AsyncPolicyWrap<(TResult Result, HttpStatusCode StatusCode)> CreateHttpAsyncUnauthorizedPolicy<TResult>(Func<TResult> defaultValueCreator)
        {
            return CreateAsyncUnauthorizedPolicy(defaultValueCreator, HttpStatusCode.Unauthorized);
        }

        private AsyncPolicyWrap<(TResult Result, System.Net.WebSockets.WebSocketCloseStatus? StatusCode)> CreateWebSocketsAsyncUnauthorizedPolicy<TResult>(Func<TResult> defaultValueCreator)
        {
            return Policy.WrapAsync(
                Policy.HandleResult<(TResult Result, System.Net.WebSockets.WebSocketCloseStatus? StatusCode)>(tuple =>
                tuple.StatusCode == System.Net.WebSockets.WebSocketCloseStatus.PolicyViolation)
                    .Or<WebSocketClosedException>(bEx => bEx.CloseStatus == System.Net.WebSockets.WebSocketCloseStatus.PolicyViolation)
                    .Or<UnauthorizedAccessException>()
                    .FallbackAsync<(TResult Result, System.Net.WebSockets.WebSocketCloseStatus? StatusCode)>((defaultValueCreator(), System.Net.WebSockets.WebSocketCloseStatus.PolicyViolation),
                        (res, c) =>
                        {
                            logger.Log(Logging.LogLevel.Error, () => unauthorizedErrorMessage);
                            throw res.Exception ?? new UnauthorizedAccessException();
                        }),
                Policy.HandleResult<(TResult Result, System.Net.WebSockets.WebSocketCloseStatus? StatusCode)>(tuple => tuple.StatusCode == System.Net.WebSockets.WebSocketCloseStatus.PolicyViolation)
                .Or<WebSocketClosedException>(bEx => bEx.CloseStatus == System.Net.WebSockets.WebSocketCloseStatus.PolicyViolation)
                    .Or<UnauthorizedAccessException>()
                    .WaitAndRetryAsync(unauthorizedRetryLoginCount,
                        i => TimeSpan.FromSeconds(i),
                        async (result, timeSpan, retryCount, context) =>
                        {
                            logger.Log(Logging.LogLevel.Warn, () => string.Format(retryUnauthorizedErrorMessage, retryCount, timeSpan));

                            if (context.TryGetValue("cancellationToken", out var cto)
                                && cto is CancellationToken ct)
                            {
                                await TryReloginAsync(ct);
                            }
                            else
                            {
                                await TryReloginAsync();
                            }
                        })
                );
        }

        private AsyncPolicyWrap<(TResult Result, TStatus StatusCode)> CreateAsyncUnauthorizedPolicy<TResult, TStatus>(Func<TResult> defaultValueCreator, TStatus unauthorized)
            where TStatus : struct
        {
            return Policy.WrapAsync(
                Policy.HandleResult<(TResult Result, TStatus StatusCode)>(tuple => 
                tuple.StatusCode.Equals(unauthorized))
                    .Or<UnauthorizedAccessException>()
                    .FallbackAsync<(TResult Result, TStatus StatusCode)>((defaultValueCreator(), unauthorized),
                        (res, c) =>
                        {
                            logger.Log(Logging.LogLevel.Error, () => unauthorizedErrorMessage);
                            throw res.Exception ?? new UnauthorizedAccessException();
                        }),
                Policy.HandleResult<(TResult Result, TStatus StatusCode)>(tuple => tuple.StatusCode.Equals(unauthorized))
                    .Or<UnauthorizedAccessException>()
                    .WaitAndRetryAsync(unauthorizedRetryLoginCount,
                        i => TimeSpan.FromSeconds(i),
                        async (result, timeSpan, retryCount, context) =>
                        {
                            logger.Log(Logging.LogLevel.Warn, () => string.Format(retryUnauthorizedErrorMessage, retryCount, timeSpan));

                            if (context.TryGetValue("cancellationToken", out var cto)
                                && cto is CancellationToken ct)
                            {
                                await TryReloginAsync(ct);
                            }
                            else
                            {
                                await TryReloginAsync();
                            }
                        })
                );
        }

        #endregion
    }
}
