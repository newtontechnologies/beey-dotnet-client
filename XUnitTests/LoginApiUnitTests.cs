using Beey.Api.Rest;
using Beey.DataExchangeModel.Auth;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace XUnitTests
{
    [CollectionDefinition("1 - Login Collection")]
    public class LoginCollectionDefinition { }


    [Collection("1 - Login Collection")]
    public class LoginApiUnitTests
    {
        private static LoginApi api;
        private static LoginToken token;

        [Fact, TestPriority(1)]
        public async Task LoginAsync()
        {
            api = new LoginApi(Configuration.BeeyUrl);
            token = await api.LoginAsync(Configuration.Email, Configuration.Password, default);
        }

        [Fact, TestPriority(2)]
        public async Task LogoutAsync()
        {
            await api.LogoutAsync(token, default);
        }
    }
}
