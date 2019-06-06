using BeeyApi.POCO;
using BeeyApi.POCO.Auth;
using BeeyApi.POCO.Projects;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace BeeyApi.Rest
{
    /// <summary>
    /// All methods:
    /// 1) returns result if everything is ok,
    /// 2) throws exception when there is some network or framework problem,
    /// 3a) throws exception when server returned 500
    /// 3b) returns null if everything is ok, but backend returned error.
    ///     Description of the error is then in the properties LastError and LastHttpStatusCode.
    /// </summary>
    public class ProjectApi : BaseAuthApi<ProjectApi>
    {
        public ProjectApi(string url) : base(url)
        {
            EndPoint = "API/Project/";
        }

        public async Task<Project> CreateAsync(string name, string customPath,
            CancellationToken cancellationToken)
        {
            return await CreateAsync(new ParamsProjectInit()
            {
                Name = name,
                CustomPath = customPath
            }, cancellationToken);
        }

        public async Task<Project> CreateAsync(ParamsProjectInit init,
            CancellationToken cancellationToken)
        {
            var result = await CreateBuilder()
                .SetBody(JsonConvert.SerializeObject(init), "application/json")
                .ExecuteAsync(HttpMethod.POST, cancellationToken);

            return HandleResponse(result, r => JsonConvert.DeserializeObject<Project>(r.GetStringContent()));
        }

        public async Task<Project?> GetAsync(int id,
            CancellationToken cancellationToken)
        {
            var result = await CreateBuilder()
                .AddParameter("id", id.ToString())
                .ExecuteAsync(HttpMethod.GET, cancellationToken);

            if (ResultNotFound(result))
            {
                return null;
            }

            return HandleResponse(result, r => JsonConvert.DeserializeObject<Project>(r.GetStringContent()));
        }

        public async Task<bool> UpdateAsync(Project project,
            CancellationToken cancellationToken)
        {
            var result = await CreateBuilder()
                .AddParameter("id", project.Id.ToString())
                .SetBody(JsonConvert.SerializeObject(project), "application/json")
                .ExecuteAsync(HttpMethod.PUT, cancellationToken);

            if (ResultNotFound(result))
            {
                return false;
            }

            return HandleResponse(result, _ => true);
        }

        public async Task<bool> DeleteAsync(int id,
            CancellationToken cancellationToken)
        {
            var result = await CreateBuilder()
                .AddParameter("id", id.ToString())
                .ExecuteAsync(HttpMethod.DELETE, cancellationToken);

            if (ResultNotFound(result))
            {
                return false;
            }

            return HandleResponse(result, _ => true);
        }

        public async Task<ProjectAccess?> GetProjectAccessAsync(int id,
            CancellationToken cancellationToken)
        {
            var result = await CreateBuilder()
                .AddUrlSegment("Access")
                .AddParameter("id", id.ToString())
                .ExecuteAsync(HttpMethod.GET, cancellationToken);

            if (ResultNotFound(result))
            {
                return null;
            }

            return HandleResponse(result, r => JsonConvert.DeserializeObject<ProjectAccess>(r.GetStringContent()));
        }

        public async Task<bool> UpdateProjectAccessAsync(ProjectAccess projectAccess,
            CancellationToken cancellationToken)
        {
            var result = await CreateBuilder()
                .AddUrlSegment("Access")
                .AddParameter("id", projectAccess.ProjectId.ToString())
                .SetBody(JsonConvert.SerializeObject(projectAccess), "application/json")
                .ExecuteAsync(HttpMethod.POST, cancellationToken);

            if (ResultNotFound(result))
            {
                return false;
            }

            return HandleResponse(result, _ => true);
        }

        public async Task<Listing<ProjectAccess>> ListProjectsAsync(int count, int skip = 0,
            OrderOn orderOn = OrderOn.Created, bool ascending = false,
            CancellationToken cancellationToken = default)
        {
            var result = await CreateBuilder()
                .AddUrlSegment("Access")
                .AddUrlSegment("List")
                .AddParameter("skip", skip)
                .AddParameter("count", count)
                .AddParameter("orderOn", GetOrderOn(orderOn))
                .AddParameter("orderBy", ascending ? "ascending" : "descending")
                .ExecuteAsync(HttpMethod.POST, cancellationToken);

            return HandleResponse(result, r => JsonConvert.DeserializeObject<Listing<ProjectAccess>>(r.GetStringContent()));
        }

        public async Task<bool> UploadTrsxAsync(int id, string fileName, byte[] trsx,
            CancellationToken cancellationToken)
        {
            System.IO.MemoryStream memoryStream;
            try { memoryStream = new System.IO.MemoryStream(trsx); }
            catch (Exception ex)
            {
                Utility.LogApiException(ex, Logger);
                throw;
            }

            try { return await UploadTrsxAsync(id, fileName, memoryStream, cancellationToken); }
            catch (Exception) { throw; }
            finally { memoryStream.Close(); }
        }

        public async Task<bool> UploadTrsxAsync(int id, string fileName, System.IO.Stream trsx,
            CancellationToken cancellationToken)
        {
            var result = await CreateBuilder()
               .AddUrlSegment("Trsx")
               .AddParameter("id", id.ToString())
               .AddFile(System.IO.Path.GetFileName(fileName), trsx)
               .ExecuteAsync(HttpMethod.POST, cancellationToken);

            return HandleResponse(result, _ => true);
        }

        public async Task<bool> ShareProjectAsync(int id, string email,
            CancellationToken cancellationToken)
        {
            var result = await CreateBuilder()
                .AddUrlSegment("Share")
                .AddParameter("id", id.ToString())
                .AddParameter("shareTo", email)
                .ExecuteAsync(HttpMethod.POST, cancellationToken);

            return HandleResponse(result, _ => true);
        }

        public async Task<Listing<ProjectAccess>> ListProjectSharing(int id,
            CancellationToken cancellationToken)
        {
            var result = await CreateBuilder()
                .AddUrlSegment("Share")
                .AddParameter("id", id.ToString())
                .ExecuteAsync(HttpMethod.GET, cancellationToken);

            return HandleResponse(result, r => JsonConvert.DeserializeObject<Listing<ProjectAccess>>(r.GetStringContent()));
        }


        public enum OrderOn { Created, Updated, None }
        private static string GetOrderOn(OrderOn orderOn)
        {
            return orderOn switch
            {
                OrderOn.Updated => "updated",
                _ => "created",
            };
        }
    }
}
