using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Beey.Client
{
    public partial class BeeyClient
    {
        public async Task<string> GetProjectDashConversionStateAsync(int projectId, CancellationToken cancellationToken)
        {
            this.RequireAuthorization();

            var policy = CreateHttpAsyncUnauthorizedPolicy<string>();
            return await policy.ExecuteAsync(async (c) =>
            {
                return await ProjectApi.GetDashConversionStateAsync(projectId, c);
            }, cancellationToken);
        }

        public async Task ConvertProjectToDashAsync(int projectId, CancellationToken cancellationToken)
        {
            this.RequireAuthorization();

            var policy = CreateHttpAsyncUnauthorizedPolicy<bool>();
            await policy.ExecuteAsync(async (c) =>
            {
                await ProjectApi.ConvertToDashAsync(projectId, c);
                return true;
            }, cancellationToken);
        }
    }
}
