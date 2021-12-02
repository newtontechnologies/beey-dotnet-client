using Beey.DataExchangeModel.Messaging;
using Beey.DataExchangeModel.Projects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Beey.Client
{
    public partial class BeeyClient
    {
        public async Task<Message[]> GetMessagesAsync(int id, DateTime? from = null, CancellationToken cancellationToken = default)
        {
            this.RequireAuthorization();

            var policy = CreateHttpAsyncUnauthorizedPolicy<Message[]>();
            return await policy.ExecuteAsync(async (c) =>
            {
                return await ProjectApi.GetMessagesAsync(id, from, c);
            }, cancellationToken);
        }

        public async Task<Project> TranscribeProjectAsync(int projectId, string language = "cs-CZ",
            bool withPpc = true, bool withVad = true, bool withPunctuation = true, bool saveTrsx = true,
            CancellationToken cancellationToken = default)
        {
            this.RequireAuthorization();

            var policy = CreateHttpAsyncUnauthorizedPolicy<Project>();
            return await policy.ExecuteAsync(async (c) =>
            {
                return await ProjectApi.TranscribeProjectAsync(projectId, language, withPpc, withVad, withPunctuation, saveTrsx, cancellationToken);
            }, cancellationToken);
        }

        public async Task<Project> EnqueueProjectAsync(int projectId, string language = "cs-CZ",
            bool withPpc = true, bool withVad = true, bool withPunctuation = true, bool saveTrsx = true,
            CancellationToken cancellationToken = default)
        {
            this.RequireAuthorization();

            var policy = CreateHttpAsyncUnauthorizedPolicy<Project>();
            return await policy.ExecuteAsync(async (c) =>
            {
                return await ProjectApi.EnqueueProjectAsync(projectId, language, withPpc, withVad, withPunctuation, saveTrsx, cancellationToken);
            }, cancellationToken);
        }

        public async Task ResetProjectAsync(int projectId, CancellationToken cancellationToken = default)
        {
            this.RequireAuthorization();

            var policy = CreateHttpAsyncUnauthorizedPolicy<bool>();
            await policy.ExecuteAsync(async (c) =>
            {
                await ProjectApi.ResetAsync(projectId, c);
                return true;
            }, cancellationToken);
        }
    }
}
