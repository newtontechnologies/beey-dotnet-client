using Beey.DataExchangeModel.Auth;
using Beey.Api;
using Polly;
using Polly.Retry;
using Polly.Wrap;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Beey.Api.Rest;
using Beey.Api.WebSockets;
using Beey.Api.Rest.Admin;
using Newtonsoft.Json.Linq;
using Beey.DataExchangeModel.Projects;
using Beey.DataExchangeModel.Lexicons;
using System.Collections.Generic;
using Beey.DataExchangeModel.Messaging;

namespace Beey.Client
{
    public partial class BeeyClient
    {
        private static readonly Logging.ILog logger = Logging.LogProvider.For<BeeyClient>();

        private string? userEmail;
        // TODO get rid of password in plaintext and still be able to re-login?
        private string? userPassword;

        protected LoginToken? LoginToken { get; set; }
        protected LoginApi LoginApi { get; set; }
        protected CurrentUserApi CurrentUserApi { get; set; }
        protected SpeakerApi SpeakerApi { get; set; }
        protected ProjectApi ProjectApi { get; set; }
        protected LexiconApi LexiconApi { get; set; }
        protected WebSocketsApi WebSocketsApi { get; set; }

        // Admin
        protected AdminUserApi AdminUserApi { get; set; }
        protected EmailApi EmailApi { get; set; }

        public BeeyClient(string url)
        {
            LoginApi = new LoginApi(url);
            CurrentUserApi = new CurrentUserApi(url);
            SpeakerApi = new SpeakerApi(url);
            ProjectApi = new ProjectApi(url);
            LexiconApi = new LexiconApi(url);

            AdminUserApi = new AdminUserApi(url);
            EmailApi = new EmailApi(url);

            string webSocketsUrl = url.Replace("http://", "ws://").Replace("https://", "wss://");
            WebSocketsApi = new WebSocketsApi(webSocketsUrl);
        }

        public async Task LoginAsync(string email, string password,
            CancellationToken cancellationToken = default)
        {
            LoginToken = await LoginApi.LoginAsync(email, password, cancellationToken);
            this.userEmail = email;
            this.userPassword = password;

            SpeakerApi.Token = LoginToken;
            ProjectApi.Token = LoginToken;
            CurrentUserApi.Token = LoginToken;
            AdminUserApi.Token = LoginToken;
            EmailApi.Token = LoginToken;
            WebSocketsApi.Token = LoginToken;
        }

        public async Task LogoutAsync(CancellationToken cancellationToken = default)
        {
            if (LoginToken != null)
            {
                await LoginApi.LogoutAsync(LoginToken, cancellationToken);
            }

            SpeakerApi.Token = null;
            ProjectApi.Token = null;
            CurrentUserApi.Token = null;
            AdminUserApi.Token = null;
            EmailApi.Token = null;
            WebSocketsApi.Token = null;

            LoginToken = null;
            userEmail = null;
            userPassword = null;
        }

        public async Task RegisterAndLoginAsync(string email, string password,
            CancellationToken cancellationToken = default)
        {
            LoginToken = await LoginApi.RegisterAndLoginAsync(email, password, cancellationToken);
            this.userEmail = email;
            this.userPassword = password;

            SpeakerApi.Token = LoginToken;
            ProjectApi.Token = LoginToken;
            CurrentUserApi.Token = LoginToken;
            AdminUserApi.Token = LoginToken;
            EmailApi.Token = LoginToken;
            WebSocketsApi.Token = LoginToken;
        }       

        public async Task<LexiconApi.TmpValidationError[]> ValidateLexiconEntryAsync(string text, string pronunciation, string language,
            CancellationToken cancellationToken = default)
        {
            this.RequireAuthorization();

            var policy = CreateHttpAsyncUnauthorizedPolicy<LexiconApi.TmpValidationError[]>();
            return await policy.ExecuteAsync(async (ctx, c) =>
            {
                return await LexiconApi.ValidateLexiconEntryAsync(text, pronunciation, language, cancellationToken);
            }, CreatePollyContext(cancellationToken), cancellationToken);
        }

        public Task<LexiconApi.TmpValidationError[]> ValidateLexiconEntryAsync(LexiconEntry entry, string language,
            CancellationToken cancellationToken = default)
            => ValidateLexiconEntryAsync(entry.Text, entry.Pronunciation, language, cancellationToken);

        public async Task<LexiconApi.TmpValidationError[]> ValidateLexiconAsync(IEnumerable<LexiconEntry> lexicon, string language,
            CancellationToken cancellationToken = default)
        {
            this.RequireAuthorization();

            var policy = CreateHttpAsyncUnauthorizedPolicy<LexiconApi.TmpValidationError[]>();
            return await policy.ExecuteAsync(async (ctx, c) =>
            {
                return await LexiconApi.ValidateLexiconAsync(lexicon, language, cancellationToken);
            }, CreatePollyContext(cancellationToken), cancellationToken);
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
                await LoginAsync(this.userEmail, this.userPassword, cancellationToken);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        #region Polly retry policies

        private const int unauthorizedRetryLoginCount = 1;
        private const string retryUnauthorizedErrorMessage = "Attempt {0} - Request was not authorized with message '{2}'. Trying to relogin and waiting {1}s before retry.";
        private const string unauthorizedErrorMessage = "Request was not authorized.";
        private Context CreatePollyContext(CancellationToken cancellationToken = default)
        {
            var result = new Context
            {
                { "cancellationToken", cancellationToken }
            };
            return result;
        }

        private AsyncPolicy<TResult> CreateWebSocketsAsyncUnauthorizedPolicy<TResult>()
        {
            return Policy<TResult>.Handle<WebSocketClosedException>(bEx => bEx.CloseStatus == System.Net.WebSockets.WebSocketCloseStatus.PolicyViolation)
                .Or<UnauthorizedAccessException>()
                .WaitAndRetryAsync(unauthorizedRetryLoginCount,
                    i => TimeSpan.FromSeconds(i),
                    async (result, timeSpan, retryCount, context) =>
                    {
                        logger.Log(Logging.LogLevel.Info, () => string.Format(retryUnauthorizedErrorMessage, retryCount, timeSpan, result.Exception.Message));

                        if (context.TryGetValue("cancellationToken", out var cto)
                            && cto is CancellationToken ct)
                        {
                            await TryReloginAsync(ct);
                        }
                        else
                        {
                            await TryReloginAsync();
                        }
                    }
                );
        }

        private AsyncPolicy<TResult> CreateHttpAsyncUnauthorizedPolicy<TResult>()
        {
            return Policy<TResult>.Handle<HttpException>(bEx => bEx.HttpStatusCode == HttpStatusCode.Unauthorized)
                .Or<UnauthorizedAccessException>()
                .WaitAndRetryAsync(unauthorizedRetryLoginCount,
                    i => TimeSpan.FromSeconds(i),
                    async (result, timeSpan, retryCount, context) =>
                    {
                        logger.Log(Logging.LogLevel.Info, () => string.Format(retryUnauthorizedErrorMessage, retryCount, timeSpan.TotalSeconds, result.Exception.Message));

                        if (context.TryGetValue("cancellationToken", out var cto)
                            && cto is CancellationToken ct)
                        {
                            await TryReloginAsync(ct);
                        }
                        else
                        {
                            await TryReloginAsync();
                        }
                    }
                );
        }

        #endregion
    }
}
