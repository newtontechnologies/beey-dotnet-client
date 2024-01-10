using System;
using System.Threading;
using System.Threading.Tasks;
using Beey.Api.DTO;
using Beey.DataExchangeModel.Projects;

namespace Beey.Client;

public partial class BeeyClient
{
    public async Task<Project> LabelTrsxAsync(int projectId,
        long accessToken,
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
        double? defaultBackgroundTransparency = null,
        CancellationToken cancellationToken = default)
    {
        RequireAuthorization();
        var policy = CreateHttpAsyncUnauthorizedPolicy<Project>();
        return await policy.ExecuteAsync(async (c) =>
        {
            return await ProjectApi.LabelTrsxAsync(projectId, accessToken, cancellationToken,
                variantId: variantId,
                subtitleLineLength: subtitleLineLength,
                keepStripped: keepStripped,
                forceSingleLine: forceSingleLine,
                enablePresegmentation: enablePresegmentation,
                speakerSignPlacement: speakerSignPlacement,
                pauseBetweenCaptionsMs: pauseBetweenCaptionsMs,
                autofillPauseBetweenCaptionsMs: autofillPauseBetweenCaptionsMs,
                useSpeakerName: useSpeakerName,
                automaticSpeed: automaticSpeed,
                minLineDurationMs: minLineDurationMs,
                ellipsis: ellipsis,
                ellipsisGapDurationMs: ellipsisGapDurationMs,
                speakerSign: speakerSign,
                feMaxDuration: feMaxDuration,
                feSpeedWarning: feSpeedWarning,
                feSpeedCriticalWarning: feSpeedCriticalWarning,
                feTemplateName: feTemplateName,
                defaultCaptionPosition: defaultCaptionPosition,
                defaultFontSize: defaultFontSize,
                defaultColor: defaultColor,
                defaultFontName: defaultFontName,
                defaultBackgroundColor: defaultBackgroundColor,
                defaultBackgroundTransparency: defaultBackgroundTransparency);
        }, cancellationToken);
    }

    public async Task<ExportFile> ExportSubtitlesWithFileFormatAsync(int projectId, string formatId,
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
        bool useSpeakerName = false,
        CancellationToken cancellationToken = default)
    {
        RequireAuthorization();
        var policy = CreateHttpAsyncUnauthorizedPolicy<ExportFile>();
        return (await policy.ExecuteAsync(async (c) =>
        {
            return await ProjectApi.ExportSubtitlesWithFileFormatAsync(projectId, formatId, c, variantId, subtitleLineLength, keepStripped, codePageNumber, diskFormatCode,
                displayStandardCode, languageCode, useBoxAroundText, forceSingleLine, speakerSignPlacement, pauseBetweenCaptionsMs, autofillPauseBetweenCaptionsMs, useSpeakerName);
        }, cancellationToken));
    }

    public async Task<ExportFile> ExportLabeledTrsxAsync(int projectId, string formatId, CancellationToken cancellationToken = default)
    {
        RequireAuthorization();
        var policy = CreateHttpAsyncUnauthorizedPolicy<ExportFile>();
        return (await policy.ExecuteAsync(async (c) =>
        {
            return await ProjectApi.ExportLabeledTrsxAsync(projectId, formatId, c);
        }, cancellationToken));
    }

}
