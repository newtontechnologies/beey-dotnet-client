using BeeyApi.POCO;
using BeeyApi.POCO.Projects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using BeeyApi.Rest;
using System.Net;

namespace BeeyUI
{
    public partial class Beey
    {
        public async Task<Project?> GetProjectAsync(int id,
            CancellationToken cancellationToken = default)
        {
            this.RequireAuthorization();

            var policy = CreateHttpAsyncUnauthorizedPolicy<Project?>();
            return (await policy.ExecuteAsync(async (ctx, c) =>
            {
                return await ProjectApi.GetAsync(id, c);
            }, CreatePollyContext(cancellationToken), cancellationToken));
        }

        public async Task<Project> CreateProjectAsync(string name, string customPath,
            CancellationToken cancellationToken = default)
        {
            return await CreateProjectAsync(new ParamsProjectInit()
            {
                Name = name,
                CustomPath = customPath
            });
        }

        public async Task<Project> CreateProjectAsync(ParamsProjectInit init,
        CancellationToken cancellationToken = default)
        {
            this.RequireAuthorization();

            var policy = CreateHttpAsyncUnauthorizedPolicy<Project>();
            return (await policy.ExecuteAsync(async (ctx, c) =>
            {
                return await ProjectApi.CreateAsync(init, c);
            }, CreatePollyContext(cancellationToken), cancellationToken));
        }

        public async Task<bool> UpdateProjectAsync(Project project,
            CancellationToken cancellationToken = default)
        {
            this.RequireAuthorization();

            var policy = CreateHttpAsyncUnauthorizedPolicy<bool>();
            return (await policy.ExecuteAsync(async (ctx, c) =>
            {
                return await ProjectApi.UpdateAsync(project, c);
            }, CreatePollyContext(cancellationToken), cancellationToken));
        }

        public async Task<bool> DeleteProjectAsync(int id,
            CancellationToken cancellationToken = default)
        {
            this.RequireAuthorization();

            var policy = CreateHttpAsyncUnauthorizedPolicy<bool>();
            return (await policy.ExecuteAsync(async (ctx, c) =>
            {
                return await ProjectApi.DeleteAsync(id, c);
            }, CreatePollyContext(cancellationToken), cancellationToken));
        }

        public async Task<Listing<Project>> ListProjectsAsync(int count = 0, int skip = 0,
            ProjectApi.OrderOn orderOn = ProjectApi.OrderOn.None, bool ascending = true,
            CancellationToken cancellationToken = default)
        {
            this.RequireAuthorization();

            var policy = CreateHttpAsyncUnauthorizedPolicy<Listing<ProjectAccess>>();
            var listing = (await policy.ExecuteAsync(async (c) =>
            {
                return await ProjectApi.ListProjectsAsync(
                    count > 0 ? count : default(int?),
                    skip >= 0 ? skip : default(int?),
                    orderOn, ascending, c);
            }, cancellationToken));

            return new Listing<Project>(listing.TotalCount, listing.ListedCount,
                listing.List.Select(p => p.Project).ToArray());
        }

        public async Task<ProjectAccess?> GetProjectAccessAsync(int id,
            CancellationToken cancellationToken = default)
        {
            this.RequireAuthorization();

            var policy = CreateHttpAsyncUnauthorizedPolicy<ProjectAccess?>();
            return (await policy.ExecuteAsync(async (c) =>
            {
                return await ProjectApi.GetProjectAccessAsync(id, c);
            }, cancellationToken));
        }

        public async Task<bool> UpdateProjectAccessAsync(ProjectAccess projectAccess,
            CancellationToken cancellationToken = default)
        {
            this.RequireAuthorization();

            var policy = CreateHttpAsyncUnauthorizedPolicy<bool>();
            return (await policy.ExecuteAsync(async (c) =>
            {
                return await ProjectApi.UpdateProjectAccessAsync(projectAccess, c);
            }, cancellationToken));
        }

        public async Task<bool> ShareProjectAsync(int id, string email,
            CancellationToken cancellationToken = default)
        {
            this.RequireAuthorization();

            var policy = CreateHttpAsyncUnauthorizedPolicy<bool>();
            return (await policy.ExecuteAsync(async (c) =>
            {
                return await ProjectApi.ShareProjectAsync(id, email, c);
            }, cancellationToken));
        }

        public async Task<Listing<ProjectAccess>> ListProjectSharingAsync(int id,
            CancellationToken cancellationToken = default)
        {
            this.RequireAuthorization();

            var policy = CreateHttpAsyncUnauthorizedPolicy<Listing<ProjectAccess>>();
            var listing = (await policy.ExecuteAsync(async (c) =>
            {
                return await ProjectApi.ListProjectSharing(id, c);
            }, cancellationToken));

            return listing;
        }

        public async Task<bool> UploadTrsxAsync(int id, string fileName, byte[] trsx,
            CancellationToken cancellationToken = default)
        {
            this.RequireAuthorization();

            var policy = CreateHttpAsyncUnauthorizedPolicy<bool>();
            return (await policy.ExecuteAsync(async (c) =>
            {
                return await ProjectApi.UploadTrsxAsync(id, fileName, trsx, c);
            }, cancellationToken));
        }

        public async Task<bool> UploadTrsxAsync(int id, string fileName, System.IO.Stream trsx,
            CancellationToken cancellationToken = default)
        {
            this.RequireAuthorization();

            var policy = CreateHttpAsyncUnauthorizedPolicy<bool>();
            return (await policy.ExecuteAsync(async (c) =>
            {
                return await ProjectApi.UploadTrsxAsync(id, fileName, trsx, c);
            }, cancellationToken));
        }
    }
}
