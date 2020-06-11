using Beey.DataExchangeModel.Messaging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Beey.Client
{
    public partial class BeeyClient
    {
        public async Task<ProjectProgress> GetProjectProgressStateAsync(int id,
          CancellationToken cancellationToken = default)
        {
            this.RequireAuthorization();

            var policy = CreateHttpAsyncUnauthorizedPolicy<ProjectProgress>();
            return await policy.ExecuteAsync(async (c) =>
            {
                return await ProjectApi.GetProgressStateAsync(id, c);
            }, cancellationToken);
        }

        public async Task<MessageNew[]> GetProjectProgressMessagesAsync(int id,
            int? count = null, int? skip = null,
            int? fromId = null, int? toId = null,
        CancellationToken cancellationToken = default)
        {
            this.RequireAuthorization();

            var policy = CreateHttpAsyncUnauthorizedPolicy<MessageNew[]>();
            return await policy.ExecuteAsync(async (c) =>
            {
                return await ProjectApi.GetProgressMessagesAsync(id, count, skip, fromId, toId, c);
            }, cancellationToken);
        }

        public async Task StopProjectAsync(int id,
           CancellationToken cancellationToken = default)
        {
            this.RequireAuthorization();

            var policy = CreateHttpAsyncUnauthorizedPolicy<bool>();
            await policy.ExecuteAsync(async (c) =>
            {
                await ProjectApi.StopAsync(id, c);
                return true;
            }, cancellationToken);
        }
    }
}
