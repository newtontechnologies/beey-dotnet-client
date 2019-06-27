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
        private const string testPassword = "ASDF___ASDF";
        private static JObject testSettings;
        private static LoginApi api;
        private static LoginToken token;

        [Fact, TestPriority(1)]
        public async Task LoginAsync()
        {
            api = new LoginApi(Configuration.BeeyUrl);
            token = await api.LoginAsync(Configuration.Email, Configuration.Password, default);
        }

        [Fact, TestPriority(2)]
        public async Task ChangePasswordAsync()
        {
            await api.ChangePasswordAsync(token, Configuration.Password, testPassword, default);
            await api.ChangePasswordAsync(token, testPassword, Configuration.Password, default);
        }

        [Fact, TestPriority(3)]
        public async Task PostUserSettingsAsync()
        {
            testSettings = JObject.Parse($"{{ Name: \"Value_{DateTime.Now}\" }}");
            await api.PostUserSettings(token, testSettings, default);
        }

        [Fact, TestPriority(4)]
        public async Task GetUserSettings()
        {
            var jSettings = await api.GetUserSettingsAsync(token, default);

            Assert.Equal(testSettings, jSettings);
        }

        [Fact, TestPriority(5)]
        public async Task LogoutAsync()
        {
            await api.LogoutAsync(token, default);
        }
    }
}
