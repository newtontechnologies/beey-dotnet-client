using Beey.Api.DTO;
using Beey.DataExchangeModel;
using Beey.DataExchangeModel.Auth;
using Beey.DataExchangeModel.Projects;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Beey.Api.Rest.Admin
{
    public class AdminUserApi : BaseAuthApi<AdminUserApi>
    {
        public AdminUserApi(string url) : base(url)
        {
            EndPoint = "API/Admin/User";
        }

        public async Task<Listing<UserViewModel>> ListAsync(int count, int skip, CancellationToken cancellationToken)
        {
            var result = await CreateBuilder()
            .AddUrlSegment("List")
            .AddParameter("skip", skip)
            .AddParameter("count", count)
            .ExecuteAsync(HttpMethod.POST, cancellationToken);

            return HandleResponse(result, r => JsonConvert.DeserializeObject<Listing<UserViewModel>>(r.GetStringContent()));
        }

        public async Task<UserViewModel> GetAsync(int id, CancellationToken cancellationToken)
        {
            var result = await CreateBuilder()
                .AddUrlSegment(id.ToString())
                .ExecuteAsync(HttpMethod.GET, cancellationToken);

            return HandleResponse(result, r => JsonConvert.DeserializeObject<UserViewModel>(r.GetStringContent()));
        }

        public async Task<UserViewModel> CreateAsync(UserAddModel user,
            CancellationToken cancellationToken)
        {
            var result = await CreateBuilder()
                .SetBody(JsonConvert.SerializeObject(user), "application/json")
                .ExecuteAsync(HttpMethod.POST, cancellationToken);

            return HandleResponse(result, r => JsonConvert.DeserializeObject<UserViewModel>(r.GetStringContent()));
        }

        public async Task UpdateAsync(UserUpdateModel user,
            CancellationToken cancellationToken)
        {
            var result = await CreateBuilder()
                .SetBody(JsonConvert.SerializeObject(user, new JsonSerializerSettings() { NullValueHandling = NullValueHandling.Ignore }), "application/json")
                .ExecuteAsync(HttpMethod.PUT, cancellationToken);

            HandleResponse(result);
        }

        public async Task<bool> DeleteAsync(int id,
            CancellationToken cancellationToken)
        {
            var result = await CreateBuilder()
                .AddUrlSegment(id.ToString())
                .ExecuteAsync(HttpMethod.DELETE, cancellationToken);

            if (ResultNotFound(result))
            {
                return false;
            }

            return HandleResponse(result, _ => true);
        }

        public async Task<Listing<MonthlyTranscriptionLogItem>> GetTranscriptionLogAsync(int id,
            CancellationToken cancellationToken)
        {
            var result = await CreateBuilder()
                .AddUrlSegment(id.ToString())
                .AddUrlSegment("TranscriptionLog")
                .ExecuteAsync(HttpMethod.GET, cancellationToken);

            return HandleResponse(result, r => JsonConvert.DeserializeObject<Listing<MonthlyTranscriptionLogItem>>(r.GetStringContent()));
        }
    }
}
