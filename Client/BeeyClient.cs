using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Beey.Api;
using Beey.Api.Rest;
using Beey.Api.Rest.Admin;
using Beey.Api.WebSockets;
using Beey.DataExchangeModel.Auth;
using Beey.DataExchangeModel.Lexicons;
using Microsoft.Extensions.Logging;
using Polly;

namespace Beey.Client;

public partial class BeeyClient
{
    private readonly ILogger<BeeyClient> logger = LoggerFactoryProvider.LoggerFactory.CreateLogger<BeeyClient>();

    private string? userEmail;

    // TODO get rid of password in plaintext and still be able to re-login?
    private string? userPassword;

    public LoginToken? LoginToken { get; protected set; }
    protected LoginApi LoginApi { get; set; }
    protected CurrentUserApi CurrentUserApi { get; set; }
    protected SpeakerApi SpeakerApi { get; set; }
    protected ProjectApi ProjectApi { get; set; }
    protected LexiconApi LexiconApi { get; set; }
    protected WebSocketsApi WebSocketsApi { get; set; }
    protected OrderApi OrderApi { get; set; }
    protected CustomApi CustomApi { get; set; }

    // Admin
    protected AdminUserApi AdminUserApi { get; set; }

    protected EmailApi EmailApi { get; set; }
    public string Url { get; }

    public BeeyClient(string url)
    {
        LoginApi = new LoginApi(url);
        CurrentUserApi = new CurrentUserApi(url);
        SpeakerApi = new SpeakerApi(url);
        ProjectApi = new ProjectApi(url);
        LexiconApi = new LexiconApi(url);
        OrderApi = new OrderApi(url);
        CustomApi = new CustomApi(url);

        AdminUserApi = new AdminUserApi(url);
        EmailApi = new EmailApi(url);

        string webSocketsUrl = url.Replace("http://", "ws://").Replace("https://", "wss://");
        WebSocketsApi = new WebSocketsApi(webSocketsUrl);
        Url = url;
    }

    private void SetTokens(LoginToken? loginToken)
    {
        LoginToken = loginToken;
        SpeakerApi.Token = loginToken;
        ProjectApi.Token = loginToken;
        CurrentUserApi.Token = loginToken;
        AdminUserApi.Token = loginToken;
        EmailApi.Token = loginToken;
        WebSocketsApi.Token = loginToken;
        OrderApi.Token = loginToken;
        CustomApi.Token = loginToken;
    }

    public Task<string> CallAsync(string route, System.Net.Http.HttpContent content, HttpMethod httpMethod,
        bool requiresAuthorization = true, CancellationToken cancellationToken = default)
    {
        return CustomApi.CallAsync(route, content, httpMethod, requiresAuthorization, cancellationToken);
    }

    public Task<string> CallAsync(string route, (string, object?)[] pars, HttpMethod httpMethod,
        bool requiresAuthorization = true, CancellationToken cancellationToken = default)
    {
        return CustomApi.CallAsync(route, pars, httpMethod, requiresAuthorization, cancellationToken);
    }

    public Task<string> CallAsync(string route, string body, string contentType, HttpMethod httpMethod,
       bool requiresAuthorization = true, CancellationToken cancellationToken = default)
    {
        return CustomApi.CallAsync(route, body, contentType, httpMethod, requiresAuthorization, cancellationToken);
    }

    public Task<string> CallAsync(string route, (string, object?)[] pars, string body, string contentType, HttpMethod httpMethod,
       bool requiresAuthorization = true, CancellationToken cancellationToken = default)
    {
        return CustomApi.CallAsync(route, pars, body, contentType, httpMethod, requiresAuthorization, cancellationToken);
    }

    public async Task<LoginToken> LoginAsync(string email, string password,
        CancellationToken cancellationToken = default)
    {
        LoginToken = await LoginApi.LoginAsync(email, password, cancellationToken);
        this.userEmail = email;
        this.userPassword = password;

        SetTokens(LoginToken);
        return LoginToken;
    }

    public async Task<LoginToken> LoginAsync(string token, CancellationToken cancellationToken = default)
    {
        LoginToken temporaryLoginToken = new LoginToken();
        temporaryLoginToken.Token = token;

        CurrentUserApi.Token = temporaryLoginToken;

        LoginToken loginToken = await CurrentUserApi.GetUserInfoAsync(cancellationToken);

        this.userEmail = loginToken.User.Email;
        // NOTE: Password is not available here, so e.g. re-login won't be possible.

        SetTokens(loginToken);
        return loginToken;
    }

    public async Task LogoutAsync(CancellationToken cancellationToken = default)
    {
        if (LoginToken != null)
        {
            await LoginApi.LogoutAsync(LoginToken, cancellationToken);
        }

        SetTokens(null);

        LoginToken = null;
        userEmail = null;
        userPassword = null;
    }

    public async Task RegisterAndLoginAsync(string email, string password, string language,
        CancellationToken cancellationToken = default)
    {
        LoginToken = await LoginApi.RegisterAndLoginAsync(email, password, language, cancellationToken);
        this.userEmail = email;
        this.userPassword = password;

        SetTokens(LoginToken);
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

    public Task<LexiconApi.TmpValidationError[]> ValidateLexiconEntryAsync(LexiconEntryDto entry, string language,
        CancellationToken cancellationToken = default)
        => ValidateLexiconEntryAsync(entry.Text, entry.IncorrectTranscription, language, cancellationToken);

    public async Task<LexiconApi.TmpValidationError[]> ValidateLexiconAsync(IEnumerable<LexiconEntryDto> lexicon, string language,
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
        if (LoginToken == null)
        {
            logger.LogError(unauthorizedErrorMessage);
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
    private const string retryUnauthorizedErrorMessage = "Attempt {retryAttempt} - Request was not authorized with message '{message}'. Trying to relogin and waiting {delay}s before retry.";
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
                    logger.LogInformation(result.Exception, retryUnauthorizedErrorMessage, retryCount, result.Exception.Message, timeSpan);

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
                    logger.LogInformation(result.Exception, retryUnauthorizedErrorMessage, retryCount, result.Exception.Message, timeSpan.TotalSeconds);

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

    #endregion Polly retry policies
}
