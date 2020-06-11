using Beey.DataExchangeModel;
using Beey.DataExchangeModel.Projects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Beey.Api.Rest;
using System.Net;
using Beey.DataExchangeModel.Messaging;
using Beey.DataExchangeModel.Export;
using System.IO;
using Newtonsoft.Json.Linq;

namespace Beey.Client
{
    public partial class BeeyClient
    {
        public async Task<Project> GetProjectAsync(int id,
            CancellationToken cancellationToken = default)
        {
            this.RequireAuthorization();

            var policy = CreateHttpAsyncUnauthorizedPolicy<Project>();
            return (await policy.ExecuteAsync(async (ctx, c) =>
            {
                return await ProjectApi.GetAsync(id, c);
            }, CreatePollyContext(cancellationToken), cancellationToken));
        }

        public async Task<Project> CreateProjectAsync(string name, string customPath,
            CancellationToken cancellationToken = default)
        {
            this.RequireAuthorization();

            var policy = CreateHttpAsyncUnauthorizedPolicy<Project>();
            return (await policy.ExecuteAsync(async (ctx, c) =>
            {
                return await ProjectApi.CreateAsync(name, customPath, c);
            }, CreatePollyContext(cancellationToken), cancellationToken));
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

        public async Task<Project> UpdateProjectAsync(Project project,
            CancellationToken cancellationToken = default)
        {
            this.RequireAuthorization();

            var policy = CreateHttpAsyncUnauthorizedPolicy<Project>();
            var result = await policy.ExecuteAsync(async (ctx, c) =>
            {
                return await ProjectApi.UpdateAsync(project, c);
            }, CreatePollyContext(cancellationToken), cancellationToken);

            return result;
        }
        public async Task<Project> UpdateProjectAsync(int id, long accessToken, string name, object value,
            CancellationToken cancellationToken = default)
        {
            this.RequireAuthorization();

            var properties = new Dictionary<string, object>() { { name, value } };

            var policy = CreateHttpAsyncUnauthorizedPolicy<Project>();
            var result = await policy.ExecuteAsync(async (ctx, c) =>
            {
                return await ProjectApi.UpdateAsync(id, accessToken, properties, c);
            }, CreatePollyContext(cancellationToken), cancellationToken);

            return result;
        }
        public async Task<Project> UpdateProjectAsync(int id, long accessToken, Dictionary<string, object> properties,
            CancellationToken cancellationToken = default)
        {
            this.RequireAuthorization();

            var policy = CreateHttpAsyncUnauthorizedPolicy<Project>();
            var result = await policy.ExecuteAsync(async (ctx, c) =>
            {
                return await ProjectApi.UpdateAsync(id, accessToken, properties, c);
            }, CreatePollyContext(cancellationToken), cancellationToken);

            return result;
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

        public async Task<Listing<Project>> ListProjectsAsync(int count, int skip = 0,
            ProjectApi.OrderOn orderOn = ProjectApi.OrderOn.None,
            bool ascending = true, DateTime? from = null, DateTime? to = null,
            CancellationToken cancellationToken = default)
        {
            var listing = await ListProjectAccessesAsync(count, skip, orderOn, ascending, from, to, cancellationToken);

            return new Listing<Project>(listing.TotalCount, listing.ListedCount,
                listing.List.Select(p => p.Project).ToArray());
        }

        public async Task<Listing<ProjectAccess>> ListProjectAccessesAsync(int count, int skip = 0,
            ProjectApi.OrderOn orderOn = ProjectApi.OrderOn.None,
            bool ascending = true, DateTime? from = null, DateTime? to = null,
            CancellationToken cancellationToken = default)
        {
            this.RequireAuthorization();

            var policy = CreateHttpAsyncUnauthorizedPolicy<Listing<ProjectAccess>>();
            var listing = (await policy.ExecuteAsync(async (c) =>
            {
                return await ProjectApi.ListProjectsAsync(count, skip, orderOn, ascending, from, to, c);
            }, cancellationToken));

            return listing;
        }
        public async Task<ProjectAccess> GetProjectAccessAsync(int id,
            CancellationToken cancellationToken = default)
        {
            this.RequireAuthorization();

            var policy = CreateHttpAsyncUnauthorizedPolicy<ProjectAccess>();
            return (await policy.ExecuteAsync(async (c) =>
            {
                return await ProjectApi.GetProjectAccessAsync(id, c);
            }, cancellationToken));
        }
        public async Task UpdateProjectAccessAsync(int projectId, ProjectAccess projectAccess,
            CancellationToken cancellationToken = default)
        {
            this.RequireAuthorization();

            var policy = CreateHttpAsyncUnauthorizedPolicy<bool>();
            await policy.ExecuteAsync(async (c) =>
            {
                await ProjectApi.UpdateProjectAccessAsync(projectId, projectAccess, c);
                return true;
            }, cancellationToken);
        }

        public async Task<Project> ShareProjectAsync(int id, string email, long accessToken,
            CancellationToken cancellationToken = default)
        {
            this.RequireAuthorization();

            var policy = CreateHttpAsyncUnauthorizedPolicy<Project>();
            return await policy.ExecuteAsync(async (c) =>
            {
                return await ProjectApi.ShareProjectAsync(id, accessToken, email, c);
            }, cancellationToken);
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

        public async Task<MessageNew[]> GetMessagesAsync(int id, DateTime? from = null, CancellationToken cancellationToken = default)
        {
            this.RequireAuthorization();

            var policy = CreateHttpAsyncUnauthorizedPolicy<MessageNew[]>();
            return await policy.ExecuteAsync(async (c) =>
            {
                return await ProjectApi.GetMessagesAsync(id, from, c);
            }, cancellationToken);
        }

        public async Task<Project> TranscribeProjectAsync(int projectId, string language = "cs-CZ",
            bool withPpc = true, bool withVad = true, bool saveTrsx = true,
            CancellationToken cancellationToken = default)
        {
            this.RequireAuthorization();

            var policy = CreateHttpAsyncUnauthorizedPolicy<Project>();
            return await policy.ExecuteAsync(async (c) =>
            {
                return await ProjectApi.TranscribeProjectAsync(projectId, language, withPpc, withVad, saveTrsx, cancellationToken);
            }, cancellationToken);
        }
        public async Task ResetProjectAsync(int projectId, CancellationToken cancellationToken = default)
        {
            this.RequireAuthorization();

            var policy = CreateHttpAsyncUnauthorizedPolicy<bool>();
            await policy.ExecuteAsync(async (c) =>
            {
                await ProjectApi.ResetAsync(projectId, c);
                return true;
            }, cancellationToken);
        }
    }
}
