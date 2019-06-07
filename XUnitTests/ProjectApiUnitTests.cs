﻿using BeeyApi.POCO.Projects;
using BeeyApi.Rest;
using BeeyApi.WebSockets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

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
        private static readonly ProjectApi projectApi = new ProjectApi(Configuration.BeeyUrl);
        private static readonly FilesApi filesApi = new FilesApi(Configuration.BeeyUrl);
        private static readonly WebSocketsApi wsApi = new WebSocketsApi(Configuration.BeeyUrl);

        private static byte[]? _testFile;
        private static byte[] testFile
        {
            get
            {
                if (_testFile == null)
                {
                    _testFile = new byte[256];
                    for (int i = 0; i < 256; i++)
                    {
                        _testFile[i] = (byte)i;
                    }
                }
                return _testFile;
            }
        }

        private static int createdProjectId;
        private static int createdProjectAccessId;

        public ProjectApiUnitTests(LoginFixture fixture)
        {
            projectApi.Token = fixture.Token;
            filesApi.Token = fixture.Token;
            wsApi.Token = fixture.Token;
        }

        #region ProjectApi
        [Fact, TestPriority(1)]
        public async Task GetNoProjectAsync()
        {
            var created = await projectApi.GetAsync(123123, default);
            Assert.True(created == null);
        }

        [Theory, TestPriority(2)]
        [InlineData(ProjectApi.OrderOn.Created, true)]
        [InlineData(ProjectApi.OrderOn.Created, false)]
        [InlineData(ProjectApi.OrderOn.Updated, true)]
        [InlineData(ProjectApi.OrderOn.Updated, false)]
        public async Task ListProjectsBy(ProjectApi.OrderOn orderOn, bool ascending)
        {
            var listing = await projectApi.ListProjectsAsync(100, 0, orderOn, ascending, default);

            List<int> ordered;

            Func<(ProjectAccess p, int i), DateTimeOffset?> orderFunc;
            switch (orderOn)
            {
                case ProjectApi.OrderOn.Created:
                    orderFunc = t => t.p.Created;
                    break;
                case ProjectApi.OrderOn.Updated:
                default:
                    orderFunc = t => t.p.Updated;
                    break;
            }

            if (ascending)
            {
                ordered = listing.List.Select((p, i) => (p, i))
                    .OrderBy(orderFunc)
                    .Select(t => t.i).ToList();
            }
            else
            {
                ordered = listing.List.Select((p, i) => (p, i))
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
        }

        [Fact, TestPriority(4)]
        public async Task ListProjectsAsync()
        {
            var listing = await projectApi.ListProjectsAsync(10, 0, ProjectApi.OrderOn.Created, true, default).TryAsync();
            Assert.True(listing);
            var created = listing.Value.List.Where(p => p.Project.Id == createdProjectId);
            Assert.Single(created);
            createdProjectAccessId = created.First().Id;
        }

        [Fact, TestPriority(5)]
        public async Task GetProjectAsync()
        {
            var created = await projectApi.GetAsync(createdProjectId, default);
            Assert.NotNull(created);
        }

        [Fact, TestPriority(6)]
        public async Task UpdateProjectAsync()
        {
            var created = await projectApi.GetAsync(createdProjectId, default);

            created!.Name = changedName;
            Assert.True(await projectApi.UpdateAsync(created, default));

            created = await projectApi.GetAsync(createdProjectId, default);
            Assert.Equal(created!.Name, changedName);
        }

        [Fact, TestPriority(6.5)]
        public async Task GetProjectAccessAsync()
        {
            var created = await projectApi.GetProjectAccessAsync(createdProjectAccessId, default);
            Assert.NotNull(created);
        }

        [Fact, TestPriority(7)]
        public async Task UpdateProjectAccessAsync()
        {
            var created = await projectApi.GetProjectAccessAsync(createdProjectAccessId, default);

            created!.CustomPath = changedName;
            Assert.True(await projectApi.UpdateProjectAccessAsync(created, default));
            created = await projectApi.GetProjectAccessAsync(createdProjectId, default);
            Assert.Equal(created!.CustomPath, changedName);
        }

        [Fact, TestPriority(8)]
        public async Task ShareProjectAsync()
        {
            Assert.True(await projectApi.ShareProjectAsync(createdProjectId, "martin.podloucky@newtontech.cz", default));
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
            Assert.True(await projectApi.UploadTrsxAsync(createdProjectId, "test", testFile, default));
        }
        #endregion

        [Fact, TestPriority(11)]
        public async Task DownloadTrsxAsync()
        {
            var project = await projectApi.GetAsync(createdProjectId, default);
            Assert.NotNull(project!.CurrentTrsxId);
            var stream = await filesApi.DownloadTrsxAsync(createdProjectId, project!.CurrentTrsxId ?? throw new Exception(), default);
            Assert.NotNull(stream);

            byte[] trsx;
            using (var ms = new System.IO.MemoryStream())
            {
                stream!.CopyTo(ms);
                trsx = ms.ToArray();
            }

            Assert.Equal(testFile, trsx);
        }

        [Fact, TestPriority(12)]
        public async Task UploadFileAsync()
        {
            Assert.True(await filesApi.UploadFileAsync(createdProjectId, "test", testFile, "cz", false, default));
        }

        [Fact, TestPriority(13)]
        public async Task DownloadFileAsync()
        {
            var project = await projectApi.GetAsync(createdProjectId, default);
            Assert.NotNull(project!.RecordingId);
            var stream = await filesApi.DownloadFileAsync(createdProjectId, project!.RecordingId ?? throw new Exception(), default);
            Assert.NotNull(stream);

            byte[] file;
            using (var ms = new System.IO.MemoryStream())
            {
                stream!.CopyTo(ms);
                file = ms.ToArray();
            }

            Assert.Equal(testFile, file);
        }

        [Fact, TestPriority(14)]
        public async Task DeleteProjectAsync()
        {
            Assert.True(await projectApi.DeleteAsync(createdProjectId, default));
        }
    }
}