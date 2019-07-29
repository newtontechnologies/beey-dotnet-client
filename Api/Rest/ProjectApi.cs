using Beey.DataExchangeModel;
using Beey.DataExchangeModel.Auth;
using Beey.DataExchangeModel.Projects;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Beey.Api.Rest
{
    /// <summary>
    /// All methods:
    /// 1) returns result if everything is ok,
    /// 2) throws exception when there is some network or framework problem,
    /// 3a) throws exception when server returned 500
    /// 3b) returns null if everything is ok, but backend returned error.
    ///     Description of the error is then in the properties LastError and LastHttpStatusCode.
    /// </summary>
    public partial class ProjectApi : BaseAuthApi<ProjectApi>
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

        public async Task<Project> GetAsync(int id,
            CancellationToken cancellationToken)
        {
            var result = await CreateBuilder()
                .AddParameter("id", id.ToString())
                .ExecuteAsync(HttpMethod.GET, cancellationToken);

            return HandleResponse(result, r => JsonConvert.DeserializeObject<Project>(r.GetStringContent()));
        }

        public async Task<Project> UpdateAsync(Project project,
            CancellationToken cancellationToken)
        {
            var result = await CreateBuilder()
                .AddParameter("id", project.Id.ToString())
                .SetBody(JsonConvert.SerializeObject(project), "application/json")
                .ExecuteAsync(HttpMethod.PUT, cancellationToken);

            return HandleResponse(result, r => JsonConvert.DeserializeObject<Project>(r.GetStringContent()));
        }

        public async Task<Project> UpdateAsync(int id, long accessToken, Dictionary<string, object> properties,
           CancellationToken cancellationToken)
        {
            if (!properties.ContainsKey("Id") && !properties.ContainsKey("id"))
            {
                properties.Add("Id", id);
            }
            if (!properties.ContainsKey("AccessToken") && !properties.ContainsKey("accessToken"))
            {
                properties.Add("AccessToken", accessToken);
            }

            var result = await CreateBuilder()
                .AddParameter("id", id.ToString())
                .SetBody(JsonConvert.SerializeObject(properties), "application/json")
                .ExecuteAsync(HttpMethod.PUT, cancellationToken);

            return HandleResponse(result, r => JsonConvert.DeserializeObject<Project>(r.GetStringContent()));
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

        public async Task<ProjectAccess> GetProjectAccessAsync(int id,
            CancellationToken cancellationToken)
        {
            var result = await CreateBuilder()
                .AddUrlSegment("Access")
                .AddParameter("id", id.ToString())
                .ExecuteAsync(HttpMethod.GET, cancellationToken);

            return HandleResponse(result, r => JsonConvert.DeserializeObject<ProjectAccess>(r.GetStringContent()));
        }

        public async Task UpdateProjectAccessAsync(ProjectAccess projectAccess,
            CancellationToken cancellationToken)
        {
            var result = await CreateBuilder()
                .AddUrlSegment("Access")
                .AddParameter("id", projectAccess.ProjectId.ToString())
                .SetBody(JsonConvert.SerializeObject(projectAccess), "application/json")
                .ExecuteAsync(HttpMethod.POST, cancellationToken);

            HandleResponse(result, _ => true);
        }

        public async Task<Listing<ProjectAccess>> ListProjectsAsync(int count, int skip,
            OrderOn orderOn, bool ascending, DateTime? from, DateTime? to,
            CancellationToken cancellationToken)
        {
            var result = await CreateBuilder()
                .AddUrlSegment("Access")
                .AddUrlSegment("List")
                .AddParameter("skip", skip)
                .AddParameter("count", count)
                .AddParameter("orderOn", GetOrderOn(orderOn))
                .AddParameter("orderBy", ascending ? "ascending" : "descending")
                .AddParameter("from", from?.ToString())
                .AddParameter("to", to?.ToString())
                .ExecuteAsync(HttpMethod.POST, cancellationToken);

            return HandleResponse(result, r => JsonConvert.DeserializeObject<Listing<ProjectAccess>>(r.GetStringContent()));
        }

        public async Task<Project> UploadTrsxAsync(int id, long accessToken, string fileName, byte[] trsx,
            CancellationToken cancellationToken)
        {
            System.IO.MemoryStream memoryStream;
            try { memoryStream = new System.IO.MemoryStream(trsx); }
            catch (Exception ex)
            {
                Utility.LogApiException(ex, Logger);
                throw;
            }

            try { return await UploadTrsxAsync(id, accessToken, fileName, memoryStream, cancellationToken); }
            catch (Exception) { throw; }
            finally { memoryStream.Close(); }
        }

        public async Task<Project> UploadTrsxAsync(int id, long accessToken, string fileName, System.IO.Stream trsx,
            CancellationToken cancellationToken)
        {
            var result = await CreateBuilder()
               .AddUrlSegment("Trsx")
               .AddParameter("id", id.ToString())
               .AddParameter("accessToken", accessToken.ToString())
               .AddFile(System.IO.Path.GetFileName(fileName), trsx)
               .ExecuteAsync(HttpMethod.POST, cancellationToken);

            return HandleResponse(result, r => JsonConvert.DeserializeObject<Project>(r.GetStringContent()));
        }

        public async Task<Project> ShareProjectAsync(int id, long accessToken, string email,
            CancellationToken cancellationToken)
        {
            var result = await CreateBuilder()
                .AddUrlSegment("Share")
                .AddParameter("id", id.ToString())
                .AddParameter("shareTo", email)
                .AddParameter("accessToken", accessToken)
                .ExecuteAsync(HttpMethod.POST, cancellationToken);

            return HandleResponse(result, r => JsonConvert.DeserializeObject<Project>(r.GetStringContent()));
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
