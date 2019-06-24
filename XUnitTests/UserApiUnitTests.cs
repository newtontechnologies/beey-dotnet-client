using Beey.Api.Rest;
using Beey.Api.Rest.Admin;
using Beey.Client;
using Beey.DataExchangeModel.Auth;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace XUnitTests
{
    [CollectionDefinition("3 - User Collection")]
    public class UserCollectionDefinition : ICollectionFixture<LoginFixture> { }

    [Collection("3 - User Collection")]
    public class UserApiUnitTests
    {
        static readonly AdminUserApi api = new AdminUserApi(Configuration.BeeyUrl);

        const string testEmail = "milos.kudelka@gmail.com";
        const string testPassword = "He55lo";

        const string changedPassword = "Pa55word";
        const int creditMinutes = 666;

        const TranscriptionCore.Speaker.Sexes testSex = TranscriptionCore.Speaker.Sexes.Male;

        private static int createdUserId;

        public UserApiUnitTests(LoginFixture fixture)
        {
            api.Token = fixture.Token;
        }

        [Fact, TestPriority(1)]
        public async Task GetNoUserAsync()
        {
            Assert.False(await api.GetAsync(-1, default).TryAsync());
        }

        [Fact, TestPriority(3)]
        public async Task ListUsersAsync()
        {
            var listing = await api.ListAsync(100, 0, default).TryAsync();
            Assert.True(listing);

            var testUser = listing.Value.List.Where(u => u.Email == testEmail).FirstOrDefault();
            if (testUser != null)
            {
                await api.DeleteAsync(testUser.Id, default);
            }
        }

        [Fact, TestPriority(4)]
        public async Task CreateUserAsync()
        {
            var user = await api.CreateAsync(new User() { Email = testEmail, Password = testPassword }, default);
            createdUserId = user.Id;
        }

        [Fact, TestPriority(5)]
        public async Task GetUserAsync()
        {
            await api.GetAsync(createdUserId, default);
        }

        [Fact, TestPriority(6)]
        public async Task UpdateUserAsync()
        {
            var user = await api.GetAsync(createdUserId, default);
     
            user.Password = testPassword;
            user.CreditMinutes = creditMinutes;
            await api.UpdateAsync(user, default);

            user = await api.GetAsync(createdUserId, default);
            Assert.Equal(creditMinutes, user.CreditMinutes);
        }

        [Fact, TestPriority(7)]
        public async Task DeleteUserAsync()
        {
            var res = await api.DeleteAsync(createdUserId, default);
            Assert.True(res);
        }
    }
}
