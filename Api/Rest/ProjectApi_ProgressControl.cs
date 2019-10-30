using Beey.DataExchangeModel;
using Beey.DataExchangeModel.Auth;
using Beey.DataExchangeModel.Messaging;
using Beey.DataExchangeModel.Projects;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
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
        public async Task<ProjectProgress> GetProgressStateAsync(int id,
           CancellationToken cancellationToken)
        {
            var result = await CreateBuilder()
                .AddUrlSegment("ProgressControl/State")
                .AddParameter("id", id)
                .ExecuteAsync(HttpMethod.GET, cancellationToken);

            return HandleResponse(result, r => JsonConvert.DeserializeObject<ProjectProgress>(r.GetStringContent()));
        }

        // TODO: Message serialization from/to JSON
        public async Task<string> GetProgressMessagesAsync(int id,
           CancellationToken cancellationToken)
        {
            var result = await CreateBuilder()
                .AddUrlSegment("ProgressControl/Messages")
                .AddParameter("id", id)
                .ExecuteAsync(HttpMethod.GET, cancellationToken);

            return HandleResponse(result, r => r.GetStringContent());
        }

        public async Task StopAsync(int id,
           CancellationToken cancellationToken)
        {
            var result = await CreateBuilder()
                .AddUrlSegment("ProgressControl/Stop")
                .AddParameter("id", id)
                .ExecuteAsync(HttpMethod.GET, cancellationToken);

            HandleResponse(result, _ => true);
        }
    }
}
