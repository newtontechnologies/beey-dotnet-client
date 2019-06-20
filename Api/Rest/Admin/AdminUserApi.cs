using Beey.DataExchangeModel;
using Beey.DataExchangeModel.Auth;
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
            EndPoint = "API/Admin/Users";
        }

        public async Task<Listing<User>> ListAsync(int count, int skip, CancellationToken cancellationToken)
        {
            var result = await CreateBuilder()
            .AddUrlSegment("List")
            .AddParameter("skip", skip)
            .AddParameter("count", count)
            .ExecuteAsync(HttpMethod.POST, cancellationToken);

            return HandleResponse(result, r => JsonConvert.DeserializeObject<Listing<User>>(r.GetStringContent()));
        }

        public async Task<User> GetAsync(int id, CancellationToken cancellationToken)
        {
            var result = await CreateBuilder()
                .AddParameter("id", id)
                .ExecuteAsync(HttpMethod.GET, cancellationToken);

            return HandleResponse(result, r => JsonConvert.DeserializeObject<User>(r.GetStringContent()));
        }

        public async Task<User> CreateAsync(User user,
            CancellationToken cancellationToken)
        {
            var result = await CreateBuilder()
                .SetBody(JsonConvert.SerializeObject(user), "application/json")
                .ExecuteAsync(HttpMethod.POST, cancellationToken);

            return HandleResponse(result, r => JsonConvert.DeserializeObject<User>(r.GetStringContent()));
        }

        public async Task UpdateAsync(User user,
            CancellationToken cancellationToken)
        {
            var result = await CreateBuilder()
                .SetBody(JsonConvert.SerializeObject(user), "application/json")
                .ExecuteAsync(HttpMethod.PUT, cancellationToken);

            HandleResponse(result, _ => true);
        }

        public async Task<bool> DeleteAsync(int id,
            CancellationToken cancellationToken)
        {
            var result = await CreateBuilder()
                .AddParameter("id", id)
                .ExecuteAsync(HttpMethod.DELETE, cancellationToken);

            if (ResultNotFound(result))
            {
                return false;
            }

            return HandleResponse(result, _ => true);
        }
    }
}
