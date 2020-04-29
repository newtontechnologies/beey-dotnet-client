using Beey.DataExchangeModel.Projects;
using Beey.Api.Rest;
using Beey.Api.WebSockets;
using Beey.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Newtonsoft.Json.Linq;
using System.IO;
using Beey.DataExchangeModel.Messaging;

namespace XUnitTests
{
    [CollectionDefinition("4 - Project Collection")]
    public class ProjectCollectionDefinition : ICollectionFixture<LoginFixture> { }

    [Collection("4 - Project Collection")]
    public class ProjectApiUnitTests
    {
        private const string testName = "test";
        private const string testPath = "test/path";
        private const string changedName = "ASDF__ASDF";
        private const string testTag = "Tag zbrusu nov";
        private const string testMetadataKey = "metadata test";
        private const string testMetadataValue = "Příliš žluťoučký kůň úpěl ďábelské ódy.";
        private static readonly ProjectApi projectApi = new ProjectApi(Configuration.BeeyUrl);
        private static readonly WebSocketsApi wsApi = new WebSocketsApi(Configuration.BeeyUrl);

        private static readonly string testMp3FilePath = Path.Combine("../../../Files", "test01.mp3");
        private static readonly string testTrsxFilePath = Path.Combine("../../../Files", "test01.trsx");
        private readonly byte[] testMp3;
        private readonly byte[] testTrsx;

        private static int createdProjectId;
        private static long createdProjectAccessToken;
        private static int createdProjectAccessId;

        public ProjectApiUnitTests(LoginFixture fixture)
        {
            projectApi.Token = fixture.Token;
            wsApi.Token = fixture.Token;


            using (var mp3 = new FileStream(testMp3FilePath, FileMode.Open, FileAccess.Read))
            using (var ms = new MemoryStream())
            {
                mp3.CopyTo(ms);
                testMp3 = ms.ToArray();
            }

            using (var trsx = new FileStream(testTrsxFilePath, FileMode.Open, FileAccess.Read))
            using (var ms = new MemoryStream())
            {
                trsx.CopyTo(ms);
                testTrsx = ms.ToArray();
            }
        }

        [Fact, TestPriority(1)]
        public async Task GetNoProjectAsync()
        {
            Assert.False(await projectApi.GetAsync(-1, default).TryAsync());
        }

        [Theory, TestPriority(2)]
        [InlineData(ProjectApi.OrderOn.Created, true)]
        [InlineData(ProjectApi.OrderOn.Created, false)]
        [InlineData(ProjectApi.OrderOn.Updated, true)]
        [InlineData(ProjectApi.OrderOn.Updated, false)]
        public async Task ListProjectsBy(ProjectApi.OrderOn orderOn, bool ascending)
        {
            var listing = await projectApi.ListProjectsAsync(100, 0, orderOn, ascending, null, null, default);

            List<int> ordered;

            Func<(ProjectAccess p, int i), DateTimeOffset?> orderFunc;
            Func<(ProjectAccess p, int i), bool> fromFunc = _ => true;
            Func<(ProjectAccess p, int i), bool> toFunc = _ => true;
            switch (orderOn)
            {
                case ProjectApi.OrderOn.Created:
                    orderFunc = t => t.p.Created;
                    break;
                case ProjectApi.OrderOn.Updated:
                default:
                    orderFunc = t => t.p.Project.Updated;
                    break;
            }

            if (ascending)
            {
                ordered = listing.List.Select((p, i) => (p, i))
                    .Where(fromFunc)
                    .Where(toFunc)
                    .OrderBy(orderFunc)
                    .Select(t => t.i).ToList();
            }
            else
            {
                ordered = listing.List.Select((p, i) => (p, i))
                    .Where(fromFunc)
                    .Where(toFunc)
                    .OrderByDescending(orderFunc)
                    .Select(p => p.i).ToList();
            }

            for (int i = 0; i < ordered.Count(); i++)
            {
                Assert.Equal(i, ordered[i]);
            }
        }

        [Fact, TestPriority(3)]
        public async Task CreateProjectAsync()
        {
            var project = await projectApi.CreateAsync($"{testName}_{DateTime.Now.ToShortTimeString()}", testPath, default);
            createdProjectId = project.Id;
            createdProjectAccessToken = project.AccessToken;
        }

        [Flags]
        public enum ListProjectsType { From, To }
        [Theory, TestPriority(4)]
        [InlineData(ListProjectsType.From)]
        [InlineData(ListProjectsType.To)]
        public async Task ListProjectsAsync(ListProjectsType listProjectsType)
        {
            DateTime? from = null;
            DateTime? to = null;
            if ((listProjectsType & ListProjectsType.From) == ListProjectsType.From)
            {
                from = DateTime.Now.AddMinutes(-1);
            }
            else if ((listProjectsType & ListProjectsType.To) == ListProjectsType.To)
            {
                to = DateTime.Now;
            }

            var listing = await projectApi.ListProjectsAsync(10, 0, ProjectApi.OrderOn.Created, true, from, to, default);

            if ((listProjectsType & ListProjectsType.From) == ListProjectsType.From)
            {
                Assert.Equal(1, listing.ListedCount);
            }
            else if ((listProjectsType & ListProjectsType.To) == ListProjectsType.To)
            {
                Assert.Equal(0, listing.ListedCount);
                return;
            }

            var created = listing.List.Where(p => p.Project.Id == createdProjectId);
            Assert.Single(created);
            createdProjectAccessId = created.First().Id;
        }

        [Fact, TestPriority(5)]
        public async Task GetProjectAsync()
        {
            var created = await projectApi.GetAsync(createdProjectId, default);
        }

        [Fact, TestPriority(6)]
        public async Task UpdateProjectAsync()
        {
            var created = await projectApi.GetAsync(createdProjectId, default);

            created!.Name = changedName;
            await projectApi.UpdateAsync(created, default);

            created = await projectApi.GetAsync(createdProjectId, default);
            createdProjectAccessToken = created.AccessToken;
            Assert.Equal(changedName, created!.Name);
        }

        [Theory, TestPriority(7)]
        [InlineData(false)]
        [InlineData(true)]
        public async Task UpdateProjectPropertyAsync(bool useAccessToken)
        {
            var res = await projectApi.UpdateAsync(createdProjectId,
                useAccessToken ? createdProjectAccessToken : -1,
                new Dictionary<string, object>() { { "Name", testName } }, default).TryAsync();

            Assert.Equal(useAccessToken, res);

            if (useAccessToken)
            {
                var created = await projectApi.GetAsync(createdProjectId, default);
                createdProjectAccessToken = created.AccessToken;
                Assert.Equal(testName, created!.Name);
            }
        }

        [Fact, TestPriority(8)]
        public async Task ShareProjectAsync()
        {
            createdProjectAccessToken = (await projectApi.ShareProjectAsync(createdProjectId, createdProjectAccessToken, "martin.podloucky@newtontech.cz", default)).AccessToken;
        }

        [Fact, TestPriority(9)]
        public async Task ListProjectSharingAsync()
        {
            var listing = await projectApi.ListProjectSharing(createdProjectId, default);
            Assert.Equal(2, listing.TotalCount);

            var sharing = listing.List.Where(sh => sh.User.Email == "martin.podloucky@newtontech.cz");
            Assert.Equal("martin.podloucky@newtontech.cz", sharing.FirstOrDefault()?.User.Email);
        }

        [Fact, TestPriority(10)]
        public async Task UploadOriginalTrsxAsync()
        {
            createdProjectAccessToken = (await projectApi.UploadOriginalTrsxAsync(createdProjectId, createdProjectAccessToken, "test01.trsx", testTrsx, default)).AccessToken;
        }

        [Fact, TestPriority(10.5)]
        public async Task DownloadOriginalTrsxAsync()
        {
            var project = await projectApi.GetAsync(createdProjectId, default);
            Assert.NotNull(project!.OriginalTrsxId);
            var stream = await projectApi.DownloadOriginalTrsxAsync(createdProjectId, default);

            byte[] trsx;
            using (var ms = new MemoryStream())
            {
                stream!.CopyTo(ms);
                trsx = ms.ToArray();
            }

            Assert.Equal(testTrsx, trsx);
        }

        [Fact, TestPriority(11)]
        public async Task UploadCurrentTrsxAsync()
        {
            createdProjectAccessToken = (await projectApi.UploadCurrentTrsxAsync(createdProjectId, createdProjectAccessToken, "test01.trsx", testTrsx, default)).AccessToken;
        }

        [Fact, TestPriority(11.5)]
        public async Task DownloadCurrentTrsxAsync()
        {
            var project = await projectApi.GetAsync(createdProjectId, default);
            Assert.NotNull(project!.CurrentTrsxId);
            var stream = await projectApi.DownloadCurrentTrsxAsync(createdProjectId, default);

            byte[] trsx;
            using (var ms = new MemoryStream())
            {
                stream!.CopyTo(ms);
                trsx = ms.ToArray();
            }

            Assert.Equal(testTrsx, trsx);
        }

        [Fact, TestPriority(12)]
        public async Task UploadFileAsync()
        {
            // TODO: filesize
            createdProjectAccessToken =
                (await projectApi.UploadMediaFileAsync(createdProjectId, testMp3.Length, "test01.mp3", testMp3,
                    default)).AccessToken;
        }

        [Fact, TestPriority(12.1)]
        public async Task GetProjectProgressStateAsync()
        {
            TryValueResult<ProjectProgress> result;
            while ((result = await projectApi.GetProgressStateAsync(createdProjectId, default).TryAsync())
                && result.Value.TranscodingState != ProcessState.Completed)
            {
                // wait
            }
        }

        [Fact, TestPriority(12.2)]
        public async Task TranscribeUploadedFileAsync()
        {
            createdProjectAccessToken = (await projectApi.TranscribeProjectAsync(createdProjectId, "cs-cz", true, true, default)).AccessToken;
        }

        [Fact, TestPriority(12.3)]
        public async Task GetProjectProgressMessagesAsync()
        {
            // TODO: message serialization not working correctly in backend
            /*
            var messages = await projectApi.GetProgressMessagesAsync(createdProjectId, null, null, null, null, default);
            Assert.True(messages.Any());
            */
        }

        [Fact, TestPriority(12.4)]
        public async Task StopProjectTranscriptionAsync()
        {
            await projectApi.StopAsync(createdProjectId, default);
        }

        [Fact, TestPriority(13)]
        public async Task DownloadFileAsync()
        {
            var project = await projectApi.GetAsync(createdProjectId, default);
            Assert.NotNull(project!.AudioRecordingId);
            var stream = await projectApi.DownloadAudioAsync(createdProjectId, default);

            byte[] file;
            using (var ms = new System.IO.MemoryStream())
            {
                stream!.CopyTo(ms);
                file = ms.ToArray();
            }

            // backend converts the file to other format and we cannot check, whether it is the same,
            // so just check, if there is something
            // TODO: make better test
            Assert.True(file.Length > 0);
        }

        [Fact, TestPriority(14)]
        public async Task UploadFileWebSocketsAsync()
        {
            await wsApi.UploadStreamAsync(createdProjectId, "test02.mp3", testMp3, testMp3.Length, false, default);
            createdProjectAccessToken = (await projectApi.GetAsync(createdProjectId, default)).AccessToken;
        }

        [Fact, TestPriority(15)]
        public async Task DownloadWebSocketFileAsync()
        {
            var project = await projectApi.GetAsync(createdProjectId, default);
            Assert.NotNull(project!.AudioRecordingId);
            var stream = await projectApi.DownloadAudioAsync(createdProjectId, default);

            byte[] file;
            using (var ms = new MemoryStream())
            {
                stream!.CopyTo(ms);
                file = ms.ToArray();
            }

            // backend converts the file to other format and we cannot check, whether it is the same,
            // so just check, if there is something
            // TODO: make better test
            Assert.True(file.Length > 0);
        }

        [Fact, TestPriority(16)]
        public async Task GetTagsAsync()
        {
            Assert.True(await projectApi.GetTagsAsync(createdProjectId, default).TryAsync());
        }

        [Fact, TestPriority(17)]

        public async Task AddTagAsync()
        {
            var project = await projectApi.AddTagAsync(createdProjectId, createdProjectAccessToken, testTag, default);
            createdProjectAccessToken = project.AccessToken;

            Assert.Contains(project.Tags, t => t.Value<string>() == testTag);
            Assert.Contains(JArray.Parse(await projectApi.GetTagsAsync(createdProjectId, default)), t => t.Value<string>() == testTag);
        }

        [Fact, TestPriority(18)]
        public async Task RemoveTagAsync()
        {
            var project = await projectApi.RemoveTagAsync(createdProjectId, createdProjectAccessToken, testTag, default);
            createdProjectAccessToken = project.AccessToken;

            Assert.DoesNotContain(project.Tags, t => t.Value<string>() == testTag);
            Assert.DoesNotContain(JArray.Parse(await projectApi.GetTagsAsync(createdProjectId, default)), t => t.Value<string>() == testTag);
        }

        [Fact, TestPriority(19)]
        public async Task GetMetadataAsync()
        {
            Assert.True(await projectApi.GetMetadataAsync(createdProjectId, testMetadataKey, default).TryAsync());
        }

        [Fact, TestPriority(20)]

        public async Task AddMetadataAsync()
        {
            var project = await projectApi.AddMetadataAsync(createdProjectId, createdProjectAccessToken,
                testMetadataKey, testMetadataValue, default);
            createdProjectAccessToken = project.AccessToken;

            var metadata = await projectApi.GetMetadataAsync(createdProjectId, testMetadataKey, default);
            Assert.NotNull(metadata);
            Assert.Equal(testMetadataValue, metadata.Value);
        }

        [Fact, TestPriority(21)]
        public async Task RemoveMetadataAsync()
        {
            var project = await projectApi.RemoveMetadataAsync(createdProjectId, createdProjectAccessToken, testMetadataKey, default);
            createdProjectAccessToken = project.AccessToken;

            Assert.Null((await projectApi.GetMetadataAsync(createdProjectId, testMetadataKey, default)).Value);
        }

        [Fact, TestPriority(22)]
        public async Task DeleteProjectAsync()
        {
            Assert.True(await projectApi.DeleteAsync(createdProjectId, default));
        }
    }
}
