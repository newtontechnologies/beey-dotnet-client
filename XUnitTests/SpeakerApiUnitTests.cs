using BeeyApi.Rest;
using System;
using System.Threading.Tasks;
using Xunit;

namespace XUnitTests
{
    [CollectionDefinition("2 - Speaker Collection")]
    public class SpeakerCollectionDefinition : ICollectionFixture<LoginFixture> { }

    [Collection("2 - Speaker Collection")]
    public class SpeakerApiUnitTests
    {
        public async Task TestAsync()
        {
            Task<int> test = Task.Run(() => -1);
            var res = await test.TryAsync();
            Task<SpeakerApi> test2 = Task.Run(() => new SpeakerApi(Configuration.BeeyUrl));
            var res2 = await test2.TryAsync();

            Task<int?> test3 = Task.Run(() => (int?)null);
            var res3 = await test3.TryAsync();
            Task<SpeakerApi?> test4 = Task.Run(() => (SpeakerApi?)null);
            var res4 = await test4.TryAsync();
        }

        static readonly SpeakerApi api = new SpeakerApi(Configuration.BeeyUrl);

        const string testFirstName = "Milošek";
        const string testSurname = "KudìlkaTýpek";

        const string changedFirstName = "ASDF__ASDF";
        const TranscriptionCore.Speaker.Sexes testSex = TranscriptionCore.Speaker.Sexes.Male;

        private static string createdSpeakerId;

        public SpeakerApiUnitTests(LoginFixture fixture)
        {
            api.Token = fixture.Token;
        }

        [Fact, TestPriority(1)]
        public async Task GetNoSpeakerAsync()
        {
            Assert.Null(await api.GetAsync("ASFDASDFASDFAFADSF_DA_F23", default));
        }

        [Fact, TestPriority(2)]
        public async Task ListNoSpeakersAsync()
        {
            var listing = await api.ListAsync(100, 0, "ASFDASDFASDFAFADSF_DA_F23", default);
            Assert.Equal(0, listing.ListedCount);
        }

        [Fact, TestPriority(3)]
        public async Task CreateSpeakerAsync()
        {
            var speaker = await api.CreateAsync(new TranscriptionCore.Speaker(testFirstName, testSurname, testSex, ""), default);
            createdSpeakerId = speaker.DBID;
        }

        [Fact, TestPriority(4)]
        public async Task ListSpeakersAsync()
        {
            var listing = await api.ListAsync(1, 0, testSurname, default);
            Assert.Equal(1, listing.ListedCount);
        }

        [Fact, TestPriority(5)]
        public async Task GetSpeakerAsync()
        {
            Assert.NotNull(await api.GetAsync(createdSpeakerId, default));
        }

        [Fact, TestPriority(6)]
        public async Task UpdateSpeakerAsync()
        {
            var speaker = await api.GetAsync(createdSpeakerId, default);

            speaker!.FirstName = changedFirstName;
            var res = await api.UpdateAsync(speaker, default);

            Assert.True(res);

            speaker = await api.GetAsync(createdSpeakerId, default);
            Assert.NotNull(speaker);
            Assert.Equal(speaker!.FirstName, changedFirstName);
        }

        [Fact, TestPriority(7)]
        public async Task DeleteSpeakerAsync()
        {
            var res = await api.DeleteAsync(createdSpeakerId, default);
            Assert.True(res);
        }
    }
}
