using Beey.Api.DTO;
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
        public async Task<ExportFormat[]> GetExportFormatsAsync(CancellationToken cancellationToken)
        {
            this.RequireAuthorization();

            var policy = CreateHttpAsyncUnauthorizedPolicy<ExportFormat[]>();
            return await policy.ExecuteAsync(async (c) =>
            {
                return await ProjectApi.GetExportFormatsAsync(1, c);
            }, cancellationToken);
        }

        public async Task<ExportFile> ExportWithFormatAsync(int projectId, string formatId,
            CancellationToken cancellationToken)
        {
            this.RequireAuthorization();

            var policy = CreateHttpAsyncUnauthorizedPolicy<ExportFile>();
            return (await policy.ExecuteAsync(async (c) =>
            {
                return await ProjectApi.ExportWithFormatAsync(projectId, formatId, c);
            }, cancellationToken));
        }
    }
}
