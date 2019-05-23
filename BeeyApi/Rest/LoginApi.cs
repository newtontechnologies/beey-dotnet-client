using BeeyApi.POCO.Auth;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BeeyApi.Rest
{
    /// <summary>
    /// All methods:
    /// 1) returns result if everything is ok,
    /// 2) throws exception when there is some network or framework problem,
    /// 3a) returns null if everything is ok, but backend returned error.
    /// 3b) throws exception when server returned 500
    ///     Description of the error is then in the properties LastError and LastHttpStatusCode..
    /// </summary>
    public class LoginApi : BaseApi<LoginApi>
    {
        public LoginApi(string url) : base(url)
        {
            EndPoint = "API/";
        }

        public async Task<LoginToken?> LoginAsync(string email, string password, CancellationToken cancellationToken)
        {
            var result = await CreateBuilder()
                .AddUrlSegment("Login")
                .AddParameters(new { email, password })
                .ExecuteAsync(HttpMethod.POST, cancellationToken);

            return HandleResponse(result, HttpStatusCode.OK,
                r => JsonConvert.DeserializeObject<LoginToken?>(r.GetStringContent()),
                _ => null);
        }

        public async Task<bool> LogoutAsync(LoginToken token, CancellationToken cancellationToken)
        {
            var result = await CreateBuilder()
                .AddUrlSegment("Logout")
                .AddHeader("Authorization", token.Token)
                .ExecuteAsync(HttpMethod.POST, cancellationToken);

            return HandleResponse(result, HttpStatusCode.OK,
                _ => true,
                _ => false);
        }

        public async Task<bool> ChangePasswordAsync(LoginToken token, string oldPassword, string newPassword, CancellationToken cancellationToken)
        {
            var result = await CreateBuilder()
                .AddUrlSegment("ChangePassword")
                .AddHeader("Authorization", token.Token)
                .AddParameters(new { password = oldPassword, newPassword })
                .ExecuteAsync(HttpMethod.POST, cancellationToken);

            return HandleResponse(result, HttpStatusCode.OK,
                _ => true,
                _ => false);
        }

        public async Task<LoginToken?> RegisterAndLoginAsync(string email, string password, CancellationToken cancellationToken)
        {
            var result = await CreateBuilder()
                .AddUrlSegment("RegisterAndLogin")
                .AddParameters(new { email, password })
                .ExecuteAsync(HttpMethod.POST, cancellationToken);

            return await HandleResponseAsync(result, HttpStatusCode.Created,
                async (r, c) => await LoginAsync(email, password, c),
                (r, c) => Task.FromResult(default(LoginToken?)),
                cancellationToken);
        }
    }
}
