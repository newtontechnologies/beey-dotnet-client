using BeeyApi.POCO;
using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TranscriptionCore;

namespace BeeyUI
{
    public partial class Beey
    {
        public async Task<Listing<Speaker>> ListSpeakersAsync(int count, int skip = 0, string? search = null,
            CancellationToken cancellationToken = default)
        {
            this.RequireAuthorization();

            var policy = CreateHttpAsyncUnauthorizedPolicy<Listing<Speaker>>();
            return (await policy.ExecuteAsync(async (ctx, c) =>
            {
                return await SpeakerApi.ListAsync(count, skip, search, c);
            }, CreatePollyContext(cancellationToken), cancellationToken));
        }

        public async Task<Speaker> GetSpeakerAsync(string dbId,
            CancellationToken cancellationToken = default)
        {
            this.RequireAuthorization();

            var policy = CreateHttpAsyncUnauthorizedPolicy<Speaker>();
            return (await policy.ExecuteAsync(async (ctx, c) =>
            {
                return await SpeakerApi.GetAsync(dbId, c);
            }, CreatePollyContext(cancellationToken), cancellationToken));
        }

        public async Task<Speaker> CreateSpeakerAsync(Speaker speaker,
            CancellationToken cancellationToken = default)
        {
            this.RequireAuthorization();

            var policy = CreateHttpAsyncUnauthorizedPolicy<Speaker>();
            return (await policy.ExecuteAsync(async (ctx, c) =>
            {
                return await SpeakerApi.CreateAsync(speaker, c);
            }, CreatePollyContext(cancellationToken), cancellationToken));
        }

        public async Task<bool> UpdateSpeakerAsync(Speaker speaker,
            CancellationToken cancellationToken = default)
        {
            this.RequireAuthorization();

            var policy = CreateHttpAsyncUnauthorizedPolicy<bool>();
            return (await policy.ExecuteAsync(async (ctx, c) =>
            {
                return await SpeakerApi.UpdateAsync(speaker, c);
            }, CreatePollyContext(cancellationToken), cancellationToken));
        }

        public async Task<bool> DeleteSpeakerAsync(string dbId,
            CancellationToken cancellationToken = default)
        {
            this.RequireAuthorization();

            var policy = CreateHttpAsyncUnauthorizedPolicy<bool>();
            return (await policy.ExecuteAsync(async (ctx, c) =>
            {
                return await SpeakerApi.DeleteAsync(dbId, c);
            }, CreatePollyContext(cancellationToken), cancellationToken));
        }
    }
}
