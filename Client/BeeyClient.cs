﻿using Beey.DataExchangeModel.Auth;
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
        protected SpeakerApi SpeakerApi { get; set; }
        protected ProjectApi ProjectApi { get; set; }
        protected FilesApi FilesApi { get; set; }
        protected WebSocketsApi WebSocketsApi { get; set; }

        public BeeyClient(string url)
        {
            LoginApi = new LoginApi(url);
            SpeakerApi = new SpeakerApi(url);
            ProjectApi = new ProjectApi(url);
            FilesApi = new FilesApi(url);

            string webSocketsUrl = url.Replace("http://", "ws://").Replace("https://", "wss://");
            WebSocketsApi = new WebSocketsApi(webSocketsUrl);
        }

        public async Task ChangePasswordAsync(string oldPassword, string newPassword,
            CancellationToken cancellationToken = default)
        {
            this.RequireAuthorization();

            var policy = CreateHttpAsyncUnauthorizedPolicy<bool>();
            await policy.ExecuteAsync(async (ctx, c) =>
            {
                await LoginApi.ChangePasswordAsync(LoginToken!, oldPassword, newPassword, cancellationToken);
                return true;
            }, CreatePollyContext(cancellationToken), cancellationToken);
        }

        public async Task LoginAsync(string email, string password,
            CancellationToken cancellationToken = default)
        {
            this.userEmail = email;
            this.userPassword = password;
            LoginToken = await LoginApi.LoginAsync(this.userEmail, this.userPassword, cancellationToken);

            SpeakerApi.Token = LoginToken;
            ProjectApi.Token = LoginToken;
            FilesApi.Token = LoginToken;
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
