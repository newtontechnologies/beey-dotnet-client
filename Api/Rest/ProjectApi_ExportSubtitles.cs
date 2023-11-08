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
    public async Task<ExportFile> ExportSubtitleWithFileFormatAsync(int projectId, string fileFormatId, CancellationToken cancellationToken,
        string? variantId = null,
        int? subtitleLineLength = null,
        bool keepStripped = false,
        string? codePageNumber = null,
        string? diskFormatCode = null,
        string? displayStandardCode = null,
        string? languageCode = null,
        string? useBoxAroundText = null,
        bool forceSingleLine = false,
        string? speakerSignPlacement = "utteranceStartOnly", // TODO: use enum from SubtitleMaker?
        int pauseBetweenCaptionsMs = 80,
        int autofillPauseBetweenCaptionsMs = 0,
        bool useSpeakerName = false)
    {
        var result = await CreateBuilder()
           .AddUrlSegment(projectId.ToString())
           .AddUrlSegment("Export/Subtitles")
           .AddParameter("fileFormatId", fileFormatId)
           .AddParameter("variantId", variantId)
           .AddParameter("subtitleLineLength", subtitleLineLength.ToString())
           .AddParameter("keepStripped", keepStripped)
           .AddParameter("codePageNumber", codePageNumber)
           .AddParameter("diskFormatCode", diskFormatCode)
           .AddParameter("displayStandardCode", displayStandardCode)
           .AddParameter("languageCode", languageCode)
           .AddParameter("useBoxAroundText", useBoxAroundText)
           .AddParameter("forceSingleLine", forceSingleLine)
           .AddParameter("speakerSignPlacement", speakerSignPlacement)
           .AddParameter("pauseBetweenCaptionsMs", pauseBetweenCaptionsMs)
           .AddParameter("autofillPauseBetweenCaptionsMs", autofillPauseBetweenCaptionsMs)
           .AddParameter("useSpeakerName", useSpeakerName)
           .ExecuteAsync(HttpMethod.GET, cancellationToken);


        return HandleResponse(result, _ =>
        {
            string mime = result.HttpResponseMessage.Content.Headers.ContentType?.MediaType ?? "application/json";
            var filename = result.HttpResponseMessage.Content.Headers.ContentDisposition?.FileName ?? "invalid.name";

            return new ExportFile(filename, mime, result.Content);
        });
    }
}

