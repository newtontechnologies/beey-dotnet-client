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

namespace XUnitTests
{
    [CollectionDefinition("3 - Project Collection")]
    public class ProjectCollectionDefinition : ICollectionFixture<LoginFixture> { }

    [Collection("3 - Project Collection")]
    public class ProjectApiUnitTests
    {
        private const string testName = "test";
        private const string testPath = "test/path";
        private const string changedName = "ASDF__ASDF";
        private const string testTag = "Tag zbrusu nov";
        private static readonly ProjectApi projectApi = new ProjectApi(Configuration.BeeyUrl);
        private static readonly FilesApi filesApi = new FilesApi(Configuration.BeeyUrl);
        private static readonly WebSocketsApi wsApi = new WebSocketsApi(Configuration.BeeyUrl);

        private static byte[] testDummyFile;
        private static readonly string testMp3FilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "../../../Files", "test01.mp3");

        private static int createdProjectId;
        private static long createdProjectAccessToken;
        private static int createdProjectAccessId;

        public ProjectApiUnitTests(LoginFixture fixture)
        {
            projectApi.Token = fixture.Token;
            filesApi.Token = fixture.Token;
            wsApi.Token = fixture.Token;

            testDummyFile = new byte[256];
            for (int i = 0; i < 256; i++)
            {
                testDummyFile[i] = (byte)i;
            }
        }

        // ProjectApi

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
        public async Task UploadTrsxAsync()
        {
            createdProjectAccessToken = (await projectApi.UploadTrsxAsync(createdProjectId, createdProjectAccessToken, "test.trsx", testDummyFile, default)).AccessToken;
        }

        // FilesApi

        [Fact, TestPriority(11)]
        public async Task DownloadTrsxAsync()
        {
            var project = await projectApi.GetAsync(createdProjectId, default);
            Assert.NotNull(project!.OriginalTrsxId);
            var stream = await filesApi.DownloadTrsxAsync(createdProjectId, project!.OriginalTrsxId ?? throw new Exception(), default);

            byte[] trsx;
            using (var ms = new System.IO.MemoryStream())
            {
                stream!.CopyTo(ms);
                trsx = ms.ToArray();
            }

            Assert.Equal(testDummyFile, trsx);
        }

        [Fact, TestPriority(12)]
        public async Task UploadFileAsync()
        {
            createdProjectAccessToken =
                (await filesApi.UploadFileAsync(createdProjectId,
                    createdProjectAccessToken,
                    new System.IO.FileInfo(testMp3FilePath),
                    "cz", false, default)).AccessToken;
        }

        [Fact, TestPriority(12.1)]
        public async Task GetProjectProgressStateAsync()
        {
            while (await projectApi.GetProgressStateAsync(createdProjectId, default).TryAsync())
            {
                // wait
            }
        }

        [Fact, TestPriority(12.2)]
        public async Task TranscribeUploadedFileAsync()
        {
            createdProjectAccessToken = (await filesApi.TranscribeProjectAsync(createdProjectId, createdProjectAccessToken, "cz", default)).AccessToken;
        }

        [Fact, TestPriority(12.3)]
        public async Task GetProjectProgressMessagesAsync()
        {
            var messages = await projectApi.GetProgressMessagesAsync(createdProjectId, default);
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
            Assert.NotNull(project!.RecordingId);
            var stream = await filesApi.DownloadFileAsync(createdProjectId, project!.RecordingId ?? throw new Exception(), default);

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
            testDummyFile[0] = 255;
            using (var ms = new System.IO.MemoryStream(testDummyFile))
            {
                await wsApi.UploadStreamAsync(createdProjectId, "test2.mp3", ms, testDummyFile.Length, "cz", false, default);
            }

            createdProjectAccessToken = (await projectApi.GetAsync(createdProjectId, default)).AccessToken;
        }

        [Fact, TestPriority(15)]
        public async Task DownloadWebSocketFileAsync()
        {
            testDummyFile[0] = 255;
            var project = await projectApi.GetAsync(createdProjectId, default);
            Assert.NotNull(project!.RecordingId);
            var stream = await filesApi.DownloadFileAsync(createdProjectId, project!.RecordingId ?? throw new Exception(), default);

            byte[] file;
            using (var ms = new System.IO.MemoryStream())
            {
                stream!.CopyTo(ms);
                file = ms.ToArray();
            }

            var res = file.SequenceEqual(testDummyFile);
            Assert.Equal(testDummyFile, file);
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
        public async Task DeleteProjectAsync()
        {
            Assert.True(await projectApi.DeleteAsync(createdProjectId, default));
        }
    }
}
