using Beey.Api.DTO;
using Beey.DataExchangeModel.Export;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Beey.Api.Rest;

partial class ProjectApi : BaseAuthApi<ProjectApi>
{
    public async Task<ExportFormat[]> GetExportFormatsAsync(int projectId, CancellationToken cancellationToken)
    {
        var result = await CreateBuilder()
           .AddUrlSegment(projectId.ToString())
           .AddUrlSegment("Export/Formats")
           .ExecuteAsync(HttpMethod.GET, cancellationToken);

        return HandleResponse(result, r => JsonSerializer.Deserialize<ExportFormat[]>(r.GetStringContent()));
    }

    public async Task<ExportFile> ExportWithFormatAsync(int projectId, string formatId,
        CancellationToken cancellationToken)
    {
        var result = await CreateBuilder()
           .AddUrlSegment(projectId.ToString())
           .AddUrlSegment("Export")
           .AddParameter("formatId", formatId)
           .ExecuteAsync(HttpMethod.GET, cancellationToken);


        return HandleResponse(result, _ =>
        {
            string mime = result.HttpResponseMessage.Content.Headers.ContentType?.MediaType ?? "application/json";
            var filename = result.HttpResponseMessage.Content.Headers.ContentDisposition?.FileName ?? "invalid.name";

            return new ExportFile(filename, mime, result.Content);
        });
    }
}
