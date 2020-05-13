using Beey.DataExchangeModel.Export;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Beey.Client
{
    public partial class BeeyClient
    {
        public async Task<ExportFormat[]> GetSubtitleExportFormatsAsync(CancellationToken cancellationToken)
        {
            this.RequireAuthorization();

            var policy = CreateHttpAsyncUnauthorizedPolicy<ExportFormat[]>();
            return await policy.ExecuteAsync(async (c) =>
            {
                return await ProjectApi.GetSubtitleExportFormatsAsync(1, c);
            }, cancellationToken);
        }

        public async Task<Stream> ExportSubtitlesAsync(int projectId, int formatId,
            CancellationToken cancellationToken)
        {
            this.RequireAuthorization();

            var policy = CreateHttpAsyncUnauthorizedPolicy<Stream>();
            return (await policy.ExecuteAsync(async (c) =>
            {
                return await ProjectApi.ExportSubtitlesAsync(projectId, formatId, c);
            }, cancellationToken));
        }
    }
}
