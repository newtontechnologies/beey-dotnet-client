using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Beey.Api.Rest
{
    public partial class ProjectApi : BaseAuthApi<ProjectApi>
    {
        public async Task<string> GetDashConversionStateAsync(int id, CancellationToken cancellationToken)
        {
            var result = await CreateBuilder()
               .AddUrlSegment(id.ToString())
               .AddUrlSegment("DashUpgrade/State")
               .ExecuteAsync(HttpMethod.GET, cancellationToken);

            return HandleResponse(result, r => r.GetStringContent());
        }

        public async Task ConvertToDashAsync(int id, CancellationToken cancellationToken)
        {
            var result = await CreateBuilder()
               .AddUrlSegment(id.ToString())
               .AddUrlSegment("DashUpgrade/Convert")
               .ExecuteAsync(HttpMethod.GET, cancellationToken);

            HandleResponse(result);
        }
    }
}
