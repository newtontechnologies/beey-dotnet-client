using BeeyApi.Rest;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace XUnitTests
{
    [Collection("1")]
    public class LoginApiUnitTests
    {
        [Fact]
        public async Task LoginAsync()
        {
            LoginApi api = new LoginApi(Configuration.BeeyUrl);
            Assert.True(await api.LoginAsync(Configuration.Email, Configuration.Password, default).TryAsync());
        }

        [Fact]
        public async Task LogoutAsync()
        {
            LoginApi api = new LoginApi(Configuration.BeeyUrl);
            var token = await api.LoginAsync(Configuration.Email, Configuration.Password, default);

            Assert.True(await api.LogoutAsync(token, default).TryAsync());
        }

        [Fact]
        public async Task RegisterAndLoginAsync()
        {
            LoginApi api = new LoginApi(Configuration.BeeyUrl);

            var token = await api.LoginAsync(Configuration.Email, Configuration.Password, default);
            Assert.True(await api.ChangePasswordAsync(token, Configuration.Password, "test", default).TryAsync());
            await api.LogoutAsync(token, default);

            await api.LoginAsync(Configuration.Email, "test", default);
            Assert.True(await api.ChangePasswordAsync(token, "test", Configuration.Password, default).TryAsync());
            await api.LogoutAsync(token, default);
        }
    }
}
