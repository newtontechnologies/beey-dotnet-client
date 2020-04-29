using Beey.Api.Rest;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace XUnitTests
{
    [CollectionDefinition("2 - Current User Collection")]
    public class CurrentUserCollectionDefinition : ICollectionFixture<LoginFixture> { }

    [Collection("2 - Current User Collection")]
    public class CurrentUserApiUnitTests
    {
        private const string testPassword = "ASDF___ASDF";
        private static JObject testSettings;
        private static readonly CurrentUserApi api = new CurrentUserApi(Configuration.BeeyUrl);

        public CurrentUserApiUnitTests(LoginFixture fixture)
        {
            api.Token = fixture.Token;
        }

        [Fact, TestPriority(2)]
        public async Task ChangePasswordAsync()
        {
            await api.ChangePasswordAsync(Configuration.Password, testPassword, default);
            await api.ChangePasswordAsync(testPassword, Configuration.Password, default);
        }

        [Fact, TestPriority(3)]
        public async Task PostUserSettingsAsync()
        {
            testSettings = JObject.Parse($"{{ Name: \"Value_{DateTime.Now}\" }}");
            await api.PostUserSettingsAsync(testSettings, default);
        }

        [Fact, TestPriority(4)]
        public async Task GetUserSettings()
        {
            var jSettings = await api.GetUserSettingsAsync(default);

            Assert.Equal(testSettings, jSettings);
        }
    }
}
