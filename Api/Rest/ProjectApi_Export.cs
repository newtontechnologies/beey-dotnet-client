using Beey.DataExchangeModel.Export;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Beey.Api.Rest
{
    partial class ProjectApi : BaseAuthApi<ProjectApi>
    {
        public async Task<ExportFormat[]> GetSubtitleExportFormatsAsync(int projectId, CancellationToken cancellationToken)
        {
            var result = await CreateBuilder()
               .AddUrlSegment(projectId.ToString())
               .AddUrlSegment("Export/Formats")
               .ExecuteAsync(HttpMethod.GET, cancellationToken);

            return HandleResponse(result, r => JsonConvert.DeserializeObject<ExportFormat[]>(r.GetStringContent()));
        }

        public async Task<System.IO.Stream> ExportSubtitlesAsync(int projectId, string formatId,
            CancellationToken cancellationToken)
        {
            var result = await CreateBuilder()
               .AddUrlSegment(projectId.ToString())
               .AddUrlSegment("Export/Formats")
               .AddParameter("formatId", formatId)
               .ExecuteAsync(HttpMethod.GET, cancellationToken);

            return HandleResponse(result, _ => result.Content);
        }
    }
}
