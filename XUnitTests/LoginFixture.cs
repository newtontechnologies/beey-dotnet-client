using BeeyApi.POCO.Auth;
using BeeyApi.Rest;
using System;
using System.Collections.Generic;
using System.Text;

namespace XUnitTests
{
    public class LoginFixture : IDisposable
    {
        public LoginToken Token { get; private set; }
        private LoginApi api;
        public LoginFixture()
        {
            api = new LoginApi(Configuration.BeeyUrl);
            Token = api.LoginAsync(Configuration.Email, Configuration.Password, default).Result;
        }

        public void Dispose()
        {
            api.LogoutAsync(Token, default).Wait();
        }
    }
}
