using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Beey.Api.DTO;
using Beey.DataExchangeModel.Projects;

namespace Beey.Api.Rest;

partial class ProjectApi : BaseAuthApi<ProjectApi>
{
    public async Task<ExportFile> ExportSubtitlesWithFileFormatAsync(int projectId, string fileFormatId, CancellationToken cancellationToken,
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
           .AddParameter("subtitleLineLength", subtitleLineLength)
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

    public async Task<Project> LabelTrsxAsync(int projectId, long accessToken, CancellationToken cancellationToken,
        string? variantId = null,
        int? subtitleLineLength = null,
        bool keepStripped = false,
        bool forceSingleLine = false,
        bool enablePresegmentation = false,
        string? speakerSignPlacement = null,  // TODO: use enum from SubtitleMaker?
        int pauseBetweenCaptionsMs = 80,
        int autofillPauseBetweenCaptionsMs = 0,
        bool useSpeakerName = false,
        double automaticSpeed = 16.0,
        int minLineDurationMs = 2000,
        string ellipsis = "... ",
        int ellipsisGapDurationMs = 300,
        string speakerSign = "  -",
        TimeSpan? feMaxDuration = null,
        double? feSpeedWarning = null,
        double? feSpeedCriticalWarning = null,
        string? feTemplateName = null,
        string? defaultCaptionPosition = null,
        string? defaultFontSize = null,
        string? defaultColor = null,
        string? defaultFontName = null,
        string? defaultBackgroundColor = null,
        double? defaultBackgroundTransparency = null)
    {
        // by default double is converted to string with comma sepparator ',' which cause problems in API as parameter, this ensure dot sepparator '.'
        var automaticSpeedStr = automaticSpeed.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var feSpeedWarningStr = feSpeedWarning is { } fsw ? fsw.ToString(System.Globalization.CultureInfo.InvariantCulture) : null;
        var feSpeedCriticalWarningStr = feSpeedCriticalWarning is { } fscw ? fscw.ToString(System.Globalization.CultureInfo.InvariantCulture) : null;
        var defaultBackgroundTransparencyStr = defaultBackgroundTransparency is { } dbt ? dbt.ToString(System.Globalization.CultureInfo.InvariantCulture) : null;
        var result = await CreateBuilder()
          .AddUrlSegment(projectId.ToString())
          .AddUrlSegment("Export/Subtitles/LabelTrsx")
          .AddParameter("accessToken", accessToken)
          .AddParameter("variantId", variantId)
          .AddParameter("subtitleLineLength", subtitleLineLength)
          .AddParameter("keepStripped", keepStripped)
          .AddParameter("forceSingleLine", forceSingleLine)
          .AddParameter("enablePresegmentation", enablePresegmentation)
          .AddParameter("speakerSignPlacement", speakerSignPlacement)
          .AddParameter("pauseBetweenCaptionsMs", pauseBetweenCaptionsMs)
          .AddParameter("autofillPauseBetweenCaptionsMs", autofillPauseBetweenCaptionsMs)
          .AddParameter("useSpeakerName", useSpeakerName)
          .AddParameter("automaticSpeed", automaticSpeedStr)
          .AddParameter("minLineDurationMs", minLineDurationMs)
          .AddParameter("ellipsis", ellipsis)
          .AddParameter("ellipsisGapDurationMs", ellipsisGapDurationMs)
          .AddParameter("speakerSign", speakerSign)
          .AddParameter("feMaxDuration", feMaxDuration)
          .AddParameter("feSpeedWarning", feSpeedWarningStr)
          .AddParameter("feSpeedCriticalWarning", feSpeedCriticalWarningStr)
          .AddParameter("feTemplateName", feTemplateName)
          .AddParameter("defaultCaptionPosition", defaultCaptionPosition)
          .AddParameter("defaultFontSize", defaultFontSize)
          .AddParameter("defaultColor", defaultColor)
          .AddParameter("defaultFontName", defaultFontName)
          .AddParameter("defaultBackgroundColor", defaultBackgroundColor)
          .AddParameter("defaultBackgroundTransparency", defaultBackgroundTransparencyStr)
          .ExecuteAsync(HttpMethod.GET, cancellationToken);

        return HandleResponse(result, r => JsonSerializer.Deserialize<Project>(r.GetStringContent()));
    }

    public async Task<ExportFile> ExportLabeledTrsxAsync(int projectId, string fileFormatId, CancellationToken cancellationToken)
    {
        var result = await CreateBuilder()
           .AddUrlSegment(projectId.ToString())
           .AddUrlSegment("Export/Subtitles")
           .AddParameter("fileFormatId", fileFormatId)
           .ExecuteAsync(HttpMethod.GET, cancellationToken);

        return HandleResponse(result, _ =>
        {
            string mime = result.HttpResponseMessage.Content.Headers.ContentType?.MediaType ?? "application/json";
            var filename = result.HttpResponseMessage.Content.Headers.ContentDisposition?.FileName ?? "invalid.name";

            return new ExportFile(filename, mime, result.Content);
        });
    }
}

