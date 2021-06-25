using Beey.DataExchangeModel.Auth;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Beey.Api.Rest
{
    public sealed class CustomApi : BaseApi<CustomApi>
    {
        public LoginToken? Token { get; set; }

        public CustomApi(string url) : base(url)
        {
        }

        private RestRequestBuilder CreateBuilder(bool requiresAuthorization)
        {
            var result = base.CreateBuilder();

            if (requiresAuthorization && Token == null) 
                throw new UnauthorizedAccessException();
            
            if (requiresAuthorization)
                result.AddHeader("Authorization", Token!.Token);

            return result;
        }

        public async Task<string> CallAsync(string route, (string, object?)[] pars, HttpMethod httpMethod, bool requiresAuthorization, CancellationToken cancellationToken)
        {
            var result = await CreateBuilder(requiresAuthorization)
                .AddUrlSegment(route.Trim('/'))
                .AddParameters(pars)
                .ExecuteAsync(httpMethod, cancellationToken);

            return HandleResponse(result, r => r.GetStringContent());
        }
    }
}
