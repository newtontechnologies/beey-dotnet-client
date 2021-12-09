using Beey.Api.Rest;
using Beey.Api.Rest.Admin;
using Beey.Client;
using Beey.DataExchangeModel.Auth;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace XUnitTests
{
    [CollectionDefinition("4 - User Collection")]
    public class C4_UserCollectionDefinition : ICollectionFixture<LoginFixture> { }

    [Collection("4 - User Collection")]
    public class C4_UserApiUnitTests
    {
        static readonly AdminUserApi api = new AdminUserApi(Configuration.BeeyUrl);

        const string testEmail = "milos.kudelka@gmail.com";
        const string testPassword = "He55lo";
        const int creditMinutes = 666;

        private static int createdUserId;

        public C4_UserApiUnitTests(LoginFixture fixture)
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
            var listing = await api.ListAsync(100, 0, default);

            var testUser = listing.List.Where(u => u.Email == testEmail).FirstOrDefault();
            if (testUser != null)
            {
                await api.DeleteAsync(testUser.Id, default);
            }
        }

        [Fact, TestPriority(4)]
        public async Task CreateUserAsync()
        {
            var user = await api.CreateAsync(new UserAddModel() { Email = testEmail, Password = testPassword }, default);
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
            await api.UpdateAsync(new UserUpdateModel()
            {
                Password = testPassword,
                CreditMinutes = creditMinutes
            },
            default);

            user = await api.GetAsync(createdUserId, default);
            Assert.Equal(creditMinutes, user.CreditMinutes);
        }

        [Fact, TestPriority(7)]
        public async Task GetTranscriptionLogAsync()
        {
            var res = await api.GetTranscriptionLogAsync(createdUserId, default);
            Assert.Empty(res.List);
        }

        [Fact, TestPriority(8)]
        public async Task DeleteUserAsync()
        {
            var res = await api.DeleteAsync(createdUserId, default);
            Assert.True(res);
        }

        [Fact, TestPriority(9)]
        public async Task AddCreditAsync()
        {
            var listing = await api.ListAsync(100, 0, default);
            var mainUser = listing.List.Where(u => u.Email == Configuration.Email).FirstOrDefault();
            await api.UpdateAsync(new UserUpdateModel()
            {
                CreditMinutes = mainUser.CreditMinutes + 10
            },
            default);
        }
    }
}
