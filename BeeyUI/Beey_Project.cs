using BeeyApi.POCO;
using BeeyApi.POCO.Projects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BeeyApi.Rest;

namespace BeeyUI
{
    public partial class Beey
    {
        public async Task<Project?> GetProjectAsync(int id,
            CancellationToken cancellationToken = default)
        {
            this.RequireAuthorization();

            return (await CreateHttpAsyncUnauthorizedPolicy<Project?>(() => null).ExecuteAsync(async (ctx, c) =>
            {
                var result = await ProjectApi.GetAsync(id, c);
                return (result, ProjectApi.LastHttpStatusCode);
            }, CreatePollyContext(cancellationToken), cancellationToken)).Result;
        }

        public async Task<Project?> CreateProjectAsync(ParamsProjectInit init,
            CancellationToken cancellationToken = default)
        {
            this.RequireAuthorization();

            var policy = CreateHttpAsyncUnauthorizedPolicy<Project?>(() => null);
            return (await policy.ExecuteAsync(async (ctx, c) =>
            {
                var result = await ProjectApi.CreateAsync(init, c);
                return (result, ProjectApi.LastHttpStatusCode);
            }, CreatePollyContext(cancellationToken), cancellationToken)).Result;
        }

        public async Task<bool> UpdateProjectAsync(Project project,
            CancellationToken cancellationToken = default)
        {
            this.RequireAuthorization();

            var policy = CreateHttpAsyncUnauthorizedPolicy(() => false);
            return (await policy.ExecuteAsync(async (ctx, c) =>
            {
                var result = await ProjectApi.UpdateAsync(project, c);
                return (result, ProjectApi.LastHttpStatusCode);
            }, CreatePollyContext(cancellationToken), cancellationToken)).Result;
        }

        public async Task<bool> DeleteProjectAsync(int id,
            CancellationToken cancellationToken = default)
        {
            this.RequireAuthorization();

            var policy = CreateHttpAsyncUnauthorizedPolicy(() => false);
            return (await policy.ExecuteAsync(async (ctx, c) =>
            {
                var result = await ProjectApi.DeleteAsync(id, c);
                return (result, ProjectApi.LastHttpStatusCode);
            }, CreatePollyContext(cancellationToken), cancellationToken)).Result;
        }

        public async Task<Listing<Project>?> ListProjectsAsync(int count = 0, int skip = 0,
            ProjectApi.OrderOn orderOn = ProjectApi.OrderOn.None, bool ascending = true,
            CancellationToken cancellationToken = default)
        {
            this.RequireAuthorization();

            var policy = CreateHttpAsyncUnauthorizedPolicy<Listing<ProjectAccess>?>(() => null);
            var listing = (await policy.ExecuteAsync(async (c) =>
            {
                var result = await ProjectApi.ListProjectsAsync(
                    count > 0 ? count : default(int?),
                    skip >= 0 ? skip : default(int?),
                    orderOn, ascending, c);
                return (result, ProjectApi.LastHttpStatusCode);
            }, cancellationToken)).Result;

            return listing != null ? new Listing<Project>(listing.TotalCount, listing.ListedCount,
                listing.List.Select(p => p.Project).ToArray()) : null;
        }

        public async Task<ProjectAccess?> GetProjectAccessAsync(int id,
            CancellationToken cancellationToken = default)
        {
            this.RequireAuthorization();

            var policy = CreateHttpAsyncUnauthorizedPolicy<ProjectAccess?>(() => null);
            return (await policy.ExecuteAsync(async (c) =>
            {
                var result = await ProjectApi.GetProjectAccessAsync(id, c);
                return (result, ProjectApi.LastHttpStatusCode);
            }, cancellationToken)).Result;
        }

        public async Task<bool> UpdateProjectAccessAsync(ProjectAccess projectAccess,
            CancellationToken cancellationToken = default)
        {
            this.RequireAuthorization();

            var policy = CreateHttpAsyncUnauthorizedPolicy(() => false);
            return (await policy.ExecuteAsync(async (c) =>
            {
                var result = await ProjectApi.UpdateProjectAccessAsync(projectAccess, c);
                return (result, ProjectApi.LastHttpStatusCode);
            }, cancellationToken)).Result;
        }

        public async Task<bool> ShareProjectAsync(int id, string email,
            CancellationToken cancellationToken = default)
        {
            this.RequireAuthorization();

            var policy = CreateHttpAsyncUnauthorizedPolicy(() => false);
            return (await policy.ExecuteAsync(async (c) =>
            {
                var result = await ProjectApi.ShareProjectAsync(id, email, c);
                return (result, ProjectApi.LastHttpStatusCode);
            }, cancellationToken)).Result;
        }

        public async Task<Listing<ProjectAccess>?> ListProjectSharingAsync(int id,
            CancellationToken cancellationToken = default)
        {
            this.RequireAuthorization();

            var policy = CreateHttpAsyncUnauthorizedPolicy<Listing<ProjectAccess>?>(() => null);
            var listing = (await policy.ExecuteAsync(async (c) =>
            {
                var result = await ProjectApi.ListProjectSharing(id, c);
                return (result, ProjectApi.LastHttpStatusCode);
            }, cancellationToken)).Result;

            return listing;
        }

        public async Task<bool> UploadTrsxAsync(int id, string fileName, byte[] trsx, CancellationToken cancellationToken = default)
        {
            this.RequireAuthorization();

            var policy = CreateHttpAsyncUnauthorizedPolicy(() => false);
            return (await policy.ExecuteAsync(async (c) =>
            {
                var result = await ProjectApi.UploadTrsxAsync(id, fileName, trsx, c);
                return (result, ProjectApi.LastHttpStatusCode);
            }, cancellationToken)).Result;
        }

        public async Task<bool> UploadTrsxAsync(int id, string fileName, System.IO.Stream trsx, CancellationToken cancellationToken = default)
        {
            this.RequireAuthorization();

            var policy = CreateHttpAsyncUnauthorizedPolicy(() => false);
            return (await policy.ExecuteAsync(async (c) =>
            {
                var result = await ProjectApi.UploadTrsxAsync(id, fileName, trsx, c);
                return (result, ProjectApi.LastHttpStatusCode);
            }, cancellationToken)).Result;
        }
    }
}
