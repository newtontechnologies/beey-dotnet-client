using System.Threading;
using System.Threading.Tasks;
using Beey.Api.DTO;
using Beey.DataExchangeModel.Projects;

namespace Beey.Client;

public partial class BeeyClient
{
    public async Task<Project> LabelTrsxAsync(int projectId, CancellationToken cancellationToken,
        string? variantId = null,
        int? subtitleLineLength = null,
        bool keepStripped = false,
        bool forceSingleLine = false,
        bool enablePresegmentation = false,
        string? speakerSignPlacement = null,  // TODO: use enum from SubtitleMaker?
        int pauseBetweenCaptionsMs = 80,
        int autofillPauseBetweenCaptionsMs = 0,
        bool useSpeakerName = false)
    {
        RequireAuthorization();
        var policy = CreateHttpAsyncUnauthorizedPolicy<Project>();
        return await policy.ExecuteAsync(async (c) =>
        {
            return await ProjectApi.LabelTrsxAsync(projectId, cancellationToken, variantId, subtitleLineLength, keepStripped, forceSingleLine,
                enablePresegmentation, speakerSignPlacement, pauseBetweenCaptionsMs, autofillPauseBetweenCaptionsMs, useSpeakerName);
        }, cancellationToken);
    }

    public async Task<ExportFile> ExportSubtitlesWithFileFormatAsync(int projectId, string formatId, CancellationToken cancellationToken,
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
        RequireAuthorization();
        var policy = CreateHttpAsyncUnauthorizedPolicy<ExportFile>();
        return (await policy.ExecuteAsync(async (c) =>
        {
            return await ProjectApi.ExportSubtitlesWithFileFormatAsync(projectId, formatId, c, variantId, subtitleLineLength, keepStripped, codePageNumber, diskFormatCode,
                displayStandardCode, languageCode, useBoxAroundText, forceSingleLine, speakerSignPlacement, pauseBetweenCaptionsMs, autofillPauseBetweenCaptionsMs, useSpeakerName);
        }, cancellationToken));
    }

    public async Task<ExportFile> ExportLabeledTrsxAsync(int projectId, string formatId, CancellationToken cancellationToken)
    {
        RequireAuthorization();
        var policy = CreateHttpAsyncUnauthorizedPolicy<ExportFile>();
        return (await policy.ExecuteAsync(async (c) =>
        {
            return await ProjectApi.ExportLabeledTrsxAsync(projectId, formatId, c);
        }, cancellationToken));
    }

}
