using Beey.DataExchangeModel.Auth;
using System;
using System.Collections.Generic;
using System.Text;

namespace BeeyApi.Rest
{
    public abstract class BaseAuthApi<TApi> : BaseApi<TApi> where TApi : BaseAuthApi<TApi>
    {
        public LoginToken? Token { get; set; }

        public BaseAuthApi(string url) : base(url)
        {
        }

        internal override RestRequestBuilder CreateBuilder()
        {
            if (Token == null) { throw new UnauthorizedAccessException(); }

            return base.CreateBuilder().AddHeader("Authorization", Token.Token);
        }
    }
}
