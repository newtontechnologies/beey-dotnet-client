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
using System.Diagnostics;

namespace XUnitTests
{
    [CollectionDefinition("7 - Project Collection")]
    public class C7_ProjectCollectionDefinition : ICollectionFixture<LoginFixture> { }

    [Collection("7 - Project Collection")]
    public class C7_ProjectApiUnitTests
    {
        private const string testName = "test";
        private const string testPath = "test/path";
        private const string changedName = "ASDF__ASDF";
        private const string testTag = "Tag zbrusu nov";
        private const string testMetadataKey = "metadata test";
        private const string testMetadataValue = "Příliš žluťoučký kůň úpěl ďábelské ódy.";
        private const string shareToEmail = "martin.podloucky@newtontech.cz";
        private static readonly ProjectApi projectApi = new ProjectApi(Configuration.BeeyUrl);
        private static readonly WebSocketsApi wsApi = new WebSocketsApi(Configuration.BeeyUrl);

        private static readonly string testMp3FilePath = Path.Combine("../../../Files", "test01.mp3");
        private static readonly string testTrsxFilePath = Path.Combine("../../../Files", "test01.trsx");
        private readonly byte[] testMp3;
        private readonly byte[] testTrsx;

        private static int createdProjectId;
        private static long createdProjectAccessToken;
        private static int createdProjectAccessId;

        public C7_ProjectApiUnitTests(LoginFixture fixture)
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
        public async Task T01_GetNoProjectAsync()
        {
            Assert.False(await projectApi.GetAsync(-1, default).TryAsync());
        }

        [Theory, TestPriority(2)]
        [InlineData(ProjectApi.OrderOn.Created, true)]
        [InlineData(ProjectApi.OrderOn.Created, false)]
        [InlineData(ProjectApi.OrderOn.Updated, true)]
        [InlineData(ProjectApi.OrderOn.Updated, false)]
        public async Task T02_ListProjectsBy(ProjectApi.OrderOn orderOn, bool ascending)
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
        public async Task T03_CreateProjectAsync()
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
        public async Task T04_ListProjectsAsync(ListProjectsType listProjectsType)
        {
            DateTime? from = null;
            DateTime? to = null;
            if ((listProjectsType & ListProjectsType.From) == ListProjectsType.From)
            {
                from = DateTime.Now.AddSeconds(-20);
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
        public async Task T05_0_GetProjectAsync()
        {
            var created = await projectApi.GetAsync(createdProjectId, default);
        }

        [Fact, TestPriority(5.3)]
        public async Task T05_3_GetProjectAccessAsync()
        {
            var created = await projectApi.GetProjectAccessAsync(createdProjectId, default);
        }

        [Fact, TestPriority(5.7)]
        public async Task T05_7_UpdateProjectAccessAsync()
        {
            var created = await projectApi.GetProjectAccessAsync(createdProjectId, default);
            created.CustomPath = "asdf";
            await projectApi.UpdateProjectAccessAsync(createdProjectId, created, default);
            created = await projectApi.GetProjectAccessAsync(createdProjectId, default);

            Assert.Equal("asdf", created.CustomPath);
        }

        [Fact, TestPriority(6)]
        public async Task T06_UpdateProjectAsync()
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
        public async Task T07_UpdateProjectPropertyAsync(bool useAccessToken)
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
        public async Task T08_ShareProjectAsync()
        {
            createdProjectAccessToken = (await projectApi.ShareProjectAsync(createdProjectId, createdProjectAccessToken, "martin.podloucky@newtontech.cz", default)).AccessToken;
        }

        [Fact, TestPriority(9)]
        public async Task T09_ListProjectSharingAsync()
        {
            var listing = await projectApi.ListProjectSharing(createdProjectId, default);
            Assert.Equal(2, listing.TotalCount);

            var sharing = listing.List.Where(sh => sh.User.Email == shareToEmail);
            Assert.Equal(shareToEmail, sharing.FirstOrDefault()?.User.Email);
        }

        [Fact, TestPriority(12)]
        public async Task T12_0_UploadFileAndWaitUntilTranscodedAsync()
        {
            await wsApi.UploadStreamAsync(createdProjectId, "test01.mp3", testMp3, testMp3.Length, false, default);
            await WaitForTranscodedAsync();
            var project = await projectApi.GetAsync(createdProjectId, default);
            createdProjectAccessToken = project.AccessToken;
            Assert.NotNull(project.MediaFileId);
        }

        [Fact, TestPriority(12.2)]
        public async Task T12_2_TranscribeUploadedFileAsync()
        {
            createdProjectAccessToken = (await projectApi.TranscribeProjectAsync(createdProjectId, "cs-CZ", true, true, true, true, default)).AccessToken;
        }

        [Fact, TestPriority(12.3)]
        public async Task T12_3_GetProjectProgressMessagesAsync()
        {
            var messages = await projectApi.GetProgressMessagesAsync(createdProjectId, null, null, null, null, default);
            Assert.True(messages.Any());
        }

        [Fact, TestPriority(13)]
        public async Task T13_0_WaitUntilTranscribed()
        {
            TryValueResult<ProjectProgress> result;
            int retryCount = 20;
            while ((result = await projectApi.GetProgressStateAsync(createdProjectId, default).TryAsync())
                && !ProcessState.Finished.HasFlag(result.Value.SPPState)
                && retryCount > 0)
            {
                await Task.Delay(5000);
                retryCount--;
            }

            Assert.True(ProcessState.Finished.HasFlag(result.Value.SPPState));
        }

        [Fact, TestPriority(13.1)]
        public async Task T13_1_CheckOriginalTrsxAsync()
        {
            int retryCount = 3;
            TryValueResult<Project> result;
            while ((result = await projectApi.GetAsync(createdProjectId, default).TryAsync())
                && result.Value.OriginalTrsxId == null
                && retryCount > 0)
            {
                await Task.Delay(3000);
                retryCount--;
            }

            using (var origStream = await projectApi.DownloadOriginalTrsxAsync(createdProjectId, default))
            using (var origReader = new StreamReader(origStream))
            using (var testStream = new MemoryStream(testTrsx))
            using (var testReader = new StreamReader(testStream))
            {
                while (!origReader.EndOfStream)
                {
                    var origLine = await origReader.ReadLineAsync();
                    var testLine = await testReader.ReadLineAsync();
                    if (origLine!.TrimStart().StartsWith("<p>"))
                    {
                        Assert.Equal(testLine, origLine);
                    }
                }
            }
        }

        [Fact, TestPriority(13.5)]
        public async Task T13_5_ResetProjectAsync()
        {
            await projectApi.ResetAsync(createdProjectId, default);
            int retry = 3;
            TryValueResult<ProjectProgress> progressResult;
            while ((progressResult = await projectApi.GetProgressStateAsync(createdProjectId, default).TryAsync())
                && progressResult.Value.RecognitionState != ProcessState.Finished
                && retry > 0)
            {
                await Task.Delay(1000);
                retry--;
            }

            var progress = progressResult.Value;
            bool allFinished = progress.UploadState == ProcessState.Finished
                && progress.MediaIdentificationState == ProcessState.Finished
                && progress.TranscodingState == ProcessState.Finished
                && progress.RecognitionState == ProcessState.Finished
                && progress.DiarizationState == ProcessState.Finished
                && progress.SpeakerIdentificationState == ProcessState.Finished
                && progress.SPPState == ProcessState.Finished;

            Assert.True(allFinished);
        }

        [Fact, TestPriority(13.6)]
        public async Task T13_6_GetSubtitleExportFormats()
        {
            var formats = await projectApi.GetSubtitleExportFormatsAsync(createdProjectId, default);
        }

        [Fact, TestPriority(13.7)]
        public async Task T13_7_ExportSubtitles()
        {
            var export = await projectApi.ExportSubtitlesAsync(createdProjectId, "srt", default);
            Assert.True(export.ReadByte() > -1);
        }

        [Fact, TestPriority(13.80)]
        public async Task T13_805_DownloadAudio()
        {
            var audio = await projectApi.DownloadAudioInitAsync(createdProjectId, default);
            Assert.True(audio.ReadByte() > -1);
        }

        [Fact, TestPriority(13.805)]
        public async Task T13_80_DownloadMediaFile()
        {
            var media = await projectApi.DownloadMediaFileAsync(createdProjectId, default);
            Assert.True(media.ReadByte() > -1);
        }

        [Fact, TestPriority(13.81)]
        public async Task T13_81_DownloadVideo()
        {
            // TODO: uncomment when using video
            //var video = await projectApi.DownloadVideoInitAsync(createdProjectId, default);
            //Assert.True(video.ReadByte() > -1);
        }
        [Fact, TestPriority(13.82)]
        public async Task T13_82_DownloadManfiest()
        {
            var manifest = await projectApi.DownloadMpdManifestAsync(createdProjectId, default);
            Assert.True(manifest.ReadByte() > -1);
        }

        [Fact, TestPriority(14)]
        public async Task T14_0_LegacyUploadFileAndWaitUntilTranscodedAsync()
        {
            _ = await projectApi.UploadMediaFileAsync(createdProjectId, testMp3.Length, "test02.mp3", testMp3, default);
            await WaitForTranscodedAsync();
        }

        [Fact, TestPriority(14.5)]
        public async Task T14_5_GetMessageCacheAsync()
        {
            var messages = await projectApi.GetMessagesAsync(createdProjectId, null, default);
            Assert.True(messages.Any());
        }

        [Fact, TestPriority(15)]
        public async Task T15_0_StartTranscribingAndStopAsync()
        {
            createdProjectAccessToken = (await projectApi.TranscribeProjectAsync(createdProjectId, "cs-CZ", true, true, true, true, default)).AccessToken;

            int retryCount = 10;
            TryValueResult<ProjectProgress> result;

            while ((result = await projectApi.GetProgressStateAsync(createdProjectId, default).TryAsync())
                            && result.Value.RecognitionState != ProcessState.Running
                            && retryCount > 0)
            {
                await Task.Delay(1000);
                retryCount--;
            }
            Assert.True(retryCount > 0);

            await projectApi.StopAsync(createdProjectId, default);

            retryCount = 5;
            while ((result = await projectApi.GetProgressStateAsync(createdProjectId, default).TryAsync())
                && !ProcessState.Finished.HasFlag(result.Value.RecognitionState)
                && retryCount > 0)
            {
                await Task.Delay(1000);
                retryCount--;
            }

            createdProjectAccessToken = (await projectApi.GetAsync(createdProjectId, default)).AccessToken;

            Assert.True(retryCount > 0);
        }

        [Fact, TestPriority(15.1)]
        public async Task T15_1_UploadOriginalTrsxAsync()
        {
            createdProjectAccessToken = (await projectApi.UploadOriginalTrsxAsync(createdProjectId, createdProjectAccessToken, "test01.trsx", testTrsx, default)).AccessToken;
        }

        [Fact, TestPriority(15.2)]
        public async Task T15_2_DownloadOriginalTrsxAsync()
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

        [Fact, TestPriority(15.3)]
        public async Task T15_3_UploadCurrentTrsxAsync()
        {
            createdProjectAccessToken = (await projectApi.UploadCurrentTrsxAsync(createdProjectId, createdProjectAccessToken, "test01.trsx", testTrsx, default)).AccessToken;
        }

        [Fact, TestPriority(15.4)]
        public async Task T15_4_DownloadCurrentTrsxAsync()
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

        [Fact, TestPriority(16)]
        public async Task T16_GetTagsAsync()
        {
            Assert.True(await projectApi.GetTagsAsync(createdProjectId, default).TryAsync());
        }

        [Fact, TestPriority(17)]

        public async Task T17_AddTagAsync()
        {
            var project = await projectApi.AddTagAsync(createdProjectId, createdProjectAccessToken, testTag, default);
            createdProjectAccessToken = project.AccessToken;

            Assert.Contains(project.Tags, t => t.Value<string>() == testTag);
            Assert.Contains(JArray.Parse(await projectApi.GetTagsAsync(createdProjectId, default)), t => t.Value<string>() == testTag);
        }

        [Fact, TestPriority(18)]
        public async Task T18_RemoveTagAsync()
        {
            var project = await projectApi.RemoveTagAsync(createdProjectId, createdProjectAccessToken, testTag, default);
            createdProjectAccessToken = project.AccessToken;

            Assert.DoesNotContain(project.Tags, t => t.Value<string>() == testTag);
            Assert.DoesNotContain(JArray.Parse(await projectApi.GetTagsAsync(createdProjectId, default)), t => t.Value<string>() == testTag);
        }

        [Fact, TestPriority(19)]
        public async Task T19_GetMetadataAsync()
        {
            Assert.True(await projectApi.GetMetadataAsync(createdProjectId, testMetadataKey, default).TryAsync());
        }

        [Fact, TestPriority(20)]

        public async Task T20_AddMetadataAsync()
        {
            var project = await projectApi.AddMetadataAsync(createdProjectId, createdProjectAccessToken,
                testMetadataKey, testMetadataValue, default);
            createdProjectAccessToken = project.AccessToken;

            var metadata = await projectApi.GetMetadataAsync(createdProjectId, testMetadataKey, default);
            Assert.NotNull(metadata);
            Assert.Equal(testMetadataValue, metadata.Value);
        }

        [Fact, TestPriority(21)]
        public async Task T21_RemoveMetadataAsync()
        {
            var project = await projectApi.RemoveMetadataAsync(createdProjectId, createdProjectAccessToken, testMetadataKey, default);
            createdProjectAccessToken = project.AccessToken;

            Assert.Null((await projectApi.GetMetadataAsync(createdProjectId, testMetadataKey, default)).Value);
        }

        [Fact, TestPriority(22)]
        public async Task T22_DeleteProjectAsync()
        {
            Assert.True(await projectApi.DeleteAsync(createdProjectId, default));
        }

        [Fact, TestPriority(23)]
        public async Task T23_DeleteSharedProject()
        {
            var loginApi = new LoginApi(Configuration.BeeyUrl);
            var projectApi = new ProjectApi(Configuration.BeeyUrl) { Token = await loginApi.LoginAsync(shareToEmail, "OVPgod", default) };
            var projects = await projectApi.ListProjectsAsync(0, 0, ProjectApi.OrderOn.None, true, null, null, default);
            foreach (var toDelete in projects.List)
            {
                Assert.True(await projectApi.DeleteAsync(toDelete.ProjectId, default));
            }
            await loginApi.LogoutAsync(projectApi.Token, default);
        }

        private async Task WaitForTranscodedAsync()
        {
            TryValueResult<ProjectProgress> result;
            int retryCount = 5;
            while ((result = await projectApi.GetProgressStateAsync(createdProjectId, default).TryAsync())
                && !ProcessState.Finished.HasFlag(result.Value.TranscodingState)
                && retryCount > 0)
            {
                // wait
                await Task.Delay(3000);
                retryCount--;
            }
            Assert.Equal(ProcessState.Completed, result.Value.TranscodingState);
        }
    }
}
