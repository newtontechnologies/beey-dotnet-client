using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Beey.DataExchangeModel.Projects;

namespace Beey.Api.Rest;

public partial class ProjectApi : BaseAuthApi<ProjectApi>
{
    public async Task<Stream> DownloadCurrentTrsxAsync(int projectId,
        CancellationToken cancellationToken)
    {
        var result = await CreateBuilder()
            .AddUrlSegment(projectId.ToString())
            .AddUrlSegment("Files/CurrentTrsx")
            .ExecuteAsync(HttpMethod.GET, cancellationToken);

        return HandleResponse(result, _ => result.Content);
    }

    public async Task<Stream> DownloadOriginalTrsxAsync(int projectId,
       CancellationToken cancellationToken)
    {
        var result = await CreateBuilder()
            .AddUrlSegment(projectId.ToString())
            .AddUrlSegment("Files/OriginalTrsx")
            .ExecuteAsync(HttpMethod.GET, cancellationToken);

        return HandleResponse(result, _ => result.Content);
    }

    public Task<ProjectDto> UploadCurrentTrsxAsync(int projectId, long accessToken, string fileName, Stream fileContent,
        CancellationToken cancellationToken)
        => UploadTrsxAsync(projectId, accessToken, false, fileName, fileContent, cancellationToken);

    public Task<ProjectDto> UploadCurrentTrsxAsync(int projectId, long accessToken, string fileName, byte[] fileContent,
        CancellationToken cancellationToken)
        => UploadTrsxAsync(projectId, accessToken, false, fileName, fileContent, cancellationToken);

    public Task<ProjectDto> UploadCurrentTrsxAsync(int projectId, long accessToken, FileInfo fileInfo,
        CancellationToken cancellationToken)
        => UploadTrsxAsync(projectId, accessToken, false, fileInfo, cancellationToken);

    public Task<ProjectDto> UploadOriginalTrsxAsync(int projectId, long accessToken, string fileName, Stream fileContent,
        CancellationToken cancellationToken)
        => UploadTrsxAsync(projectId, accessToken, true, fileName, fileContent, cancellationToken);

    public Task<ProjectDto> UploadOriginalTrsxAsync(int projectId, long accessToken, string fileName, byte[] fileContent,
        CancellationToken cancellationToken)
        => UploadTrsxAsync(projectId, accessToken, true, fileName, fileContent, cancellationToken);

    public Task<ProjectDto> UploadOriginalTrsxAsync(int projectId, long accessToken, FileInfo fileInfo,
        CancellationToken cancellationToken)
        => UploadTrsxAsync(projectId, accessToken, true, fileInfo, cancellationToken);

    private async Task<ProjectDto> UploadTrsxAsync(int projectId, long accessToken, bool original, string fileName, Stream fileContent,
        CancellationToken cancellationToken)
    {
        var builder = CreateBuilder()
            .AddUrlSegment(projectId.ToString())
            .AddUrlSegment("Files")
            .AddUrlSegment(original ? "OriginalTrsx" : "CurrentTrsx")
            .AddParameter("accessToken", accessToken.ToString())
            .AddFile(fileName, fileContent);

        var result = await builder.ExecuteAsync(HttpMethod.POST, cancellationToken);

        return HandleResponse(result, r => JsonSerializer.Deserialize<ProjectDto>(r.GetStringContent()));
    }

    private async Task<ProjectDto> UploadTrsxAsync(int projectId, long accessToken, bool original, string fileName, byte[] fileContent,
        CancellationToken cancellationToken)
    {
        MemoryStream memoryStream = CreateMemoryStream(fileContent);

        try { return await UploadTrsxAsync(projectId, accessToken, original, fileName, memoryStream, cancellationToken); }
        finally { memoryStream.Close(); }
    }

    private async Task<ProjectDto> UploadTrsxAsync(int projectId, long accessToken, bool original, FileInfo fileInfo,
        CancellationToken cancellationToken)
    {
        FileStream fileStream = CreateFileStream(fileInfo);

        try { return await UploadTrsxAsync(projectId, accessToken, original, fileInfo.Name, fileStream, cancellationToken); }
        finally { fileStream.Close(); }
    }
}
