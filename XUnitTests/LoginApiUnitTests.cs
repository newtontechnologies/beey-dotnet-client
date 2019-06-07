using BeeyApi.Rest;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace XUnitTests
{
    [Collection("1 - Login Collection")]
    public class LoginApiUnitTests
    {
        private const string testPassword = "ASDF___ASDF";

        [Fact, TestPriority(1)]
        public async Task LoginAsync()
        {
            LoginApi api = new LoginApi(Configuration.BeeyUrl);
            Assert.NotNull(await api.LoginAsync(Configuration.Email, Configuration.Password, default));
        }

        [Fact, TestPriority(2)]
        public async Task ChangePasswordAsync()
        {
            LoginApi api = new LoginApi(Configuration.BeeyUrl);
            var token = await api.LoginAsync(Configuration.Email, Configuration.Password, default);

            await api.ChangePasswordAsync(token, Configuration.Password, testPassword, default);
            await api.ChangePasswordAsync(token, testPassword, Configuration.Password, default);
        }

        [Fact, TestPriority(3)]
        public async Task LogoutAsync()
        {
            LoginApi api = new LoginApi(Configuration.BeeyUrl);
            var token = await api.LoginAsync(Configuration.Email, Configuration.Password, default);
            await api.LogoutAsync(token, default);
        }
    }
}
