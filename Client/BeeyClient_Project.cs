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

namespace Beey.Client;

public partial class BeeyClient
{
    public async Task<ProjectDto> GetProjectAsync(int id,
        CancellationToken cancellationToken = default)
    {
        this.RequireAuthorization();

        var policy = CreateHttpAsyncUnauthorizedPolicy<ProjectDto>();
        return (await policy.ExecuteAsync(async (ctx, c) =>
        {
            return await ProjectApi.GetAsync(id, c);
        }, CreatePollyContext(cancellationToken), cancellationToken));
    }

    public async Task<ProjectDto> CreateProjectAsync(string name, string? customPath,
        CancellationToken cancellationToken = default)
    {
        this.RequireAuthorization();

        var policy = CreateHttpAsyncUnauthorizedPolicy<ProjectDto>();
        return (await policy.ExecuteAsync(async (ctx, c) =>
        {
            return await ProjectApi.CreateAsync(name, customPath, c);
        }, CreatePollyContext(cancellationToken), cancellationToken));
    }

    public async Task<ProjectDto> CreateProjectAsync(ParamsProjectInit init,
    CancellationToken cancellationToken = default)
    {
        this.RequireAuthorization();

        var policy = CreateHttpAsyncUnauthorizedPolicy<ProjectDto>();
        return (await policy.ExecuteAsync(async (ctx, c) =>
        {
            return await ProjectApi.CreateAsync(init, c);
        }, CreatePollyContext(cancellationToken), cancellationToken));
    }

    public async Task<ProjectDto> UpdateProjectAsync(ProjectDto project,
        CancellationToken cancellationToken = default)
    {
        this.RequireAuthorization();

        var policy = CreateHttpAsyncUnauthorizedPolicy<ProjectDto>();
        var result = await policy.ExecuteAsync(async (ctx, c) =>
        {
            return await ProjectApi.UpdateAsync(project, c);
        }, CreatePollyContext(cancellationToken), cancellationToken);

        return result;
    }

    public async Task<ProjectDto> UpdateProjectAsync(int id, long accessToken, string name, object value,
        CancellationToken cancellationToken = default)
    {
        this.RequireAuthorization();

        var properties = new Dictionary<string, object>() { { name, value } };

        var policy = CreateHttpAsyncUnauthorizedPolicy<ProjectDto>();
        var result = await policy.ExecuteAsync(async (ctx, c) =>
        {
            return await ProjectApi.UpdateAsync(id, accessToken, properties, c);
        }, CreatePollyContext(cancellationToken), cancellationToken);

        return result;
    }

    public async Task<ProjectDto> UpdateProjectAsync(int id, long accessToken, Dictionary<string, object> properties,
        CancellationToken cancellationToken = default)
    {
        this.RequireAuthorization();

        var policy = CreateHttpAsyncUnauthorizedPolicy<ProjectDto>();
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

    public async Task<Listing<ProjectDto>> ListProjectsAsync(int count, int skip = 0,
        ProjectApi.OrderOn orderOn = ProjectApi.OrderOn.None,
        bool ascending = true, DateTime? from = null, DateTime? to = null,
        CancellationToken cancellationToken = default)
    {
        var listing = await ListProjectAccessesAsync(count, skip, orderOn, ascending, from, to, cancellationToken);

        return new Listing<ProjectDto>(listing.TotalCount, listing.ListedCount,
            listing.List.Select(p => p.Project).ToArray());
    }

    public async Task<Listing<ProjectAccessDto>> ListProjectAccessesAsync(int count, int skip = 0,
        ProjectApi.OrderOn orderOn = ProjectApi.OrderOn.None,
        bool ascending = true, DateTime? from = null, DateTime? to = null,
        CancellationToken cancellationToken = default)
    {
        this.RequireAuthorization();

        var policy = CreateHttpAsyncUnauthorizedPolicy<Listing<ProjectAccessDto>>();
        var listing = (await policy.ExecuteAsync(async (c) =>
        {
            return await ProjectApi.ListProjectsAsync(count, skip, orderOn, ascending, from, to, c);
        }, cancellationToken));

        return listing;
    }

    public async Task<ProjectAccessDto> GetProjectAccessAsync(int id,
        CancellationToken cancellationToken = default)
    {
        this.RequireAuthorization();

        var policy = CreateHttpAsyncUnauthorizedPolicy<ProjectAccessDto>();
        return (await policy.ExecuteAsync(async (c) =>
        {
            return await ProjectApi.GetProjectAccessAsync(id, c);
        }, cancellationToken));
    }

    public async Task UpdateProjectAccessAsync(int projectId, ProjectAccessUpdateModel projectAccess,
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

    public async Task<ProjectDto> ShareProjectAsync(int id, string email, long accessToken,
        CancellationToken cancellationToken = default)
    {
        this.RequireAuthorization();

        var policy = CreateHttpAsyncUnauthorizedPolicy<ProjectDto>();
        return await policy.ExecuteAsync(async (c) =>
        {
            return await ProjectApi.ShareProjectAsync(id, accessToken, email, c);
        }, cancellationToken);
    }

    public async Task<Listing<ProjectAccessDto>> ListProjectSharingAsync(int id,
        CancellationToken cancellationToken = default)
    {
        this.RequireAuthorization();

        var policy = CreateHttpAsyncUnauthorizedPolicy<Listing<ProjectAccessDto>>();
        var listing = (await policy.ExecuteAsync(async (c) =>
        {
            return await ProjectApi.ListProjectSharing(id, c);
        }, cancellationToken));

        return listing;
    }

    public async Task<ProjectDto> CopyProjectAsync(int id,
        CancellationToken cancellationToken = default)
    {
        this.RequireAuthorization();

        var policy = CreateHttpAsyncUnauthorizedPolicy<ProjectDto>();
        return await policy.ExecuteAsync(async (c) =>
        {
            return await ProjectApi.CopyProjectAsync(id, c);
        }, cancellationToken);
    }
}
