using BeeyApi.POCO;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TranscriptionCore;

namespace BeeyUI
{
    public partial class Beey
    {
        public async Task<Listing<Speaker>?> ListSpeakersAsync(int count = 0, int skip = 0, string? search = null,
            CancellationToken cancellationToken = default)
        {
            this.RequireAuthorization();

            var policy = CreateHttpAsyncUnauthorizedPolicy<Listing<Speaker>?>(() => null);
            return (await policy.ExecuteAsync(async (ctx, c) =>
            {
                var result = await SpeakerApi.ListAsync(
                    count > 0 ? count : default(int?),
                    skip >= 0 ? skip : default(int?),
                    search, c);
                return (result, SpeakerApi.LastHttpStatusCode);
            }, CreatePollyContext(cancellationToken), cancellationToken)).Result;
        }

        public async Task<Speaker?> GetSpeakerAsync(string dbId,
            CancellationToken cancellationToken = default)
        {
            this.RequireAuthorization();

            var policy = CreateHttpAsyncUnauthorizedPolicy<Speaker?>(() => null);
            return (await policy.ExecuteAsync(async (ctx, c) =>
            {
                var result = await SpeakerApi.GetAsync(dbId, c);
                return (result, SpeakerApi.LastHttpStatusCode);
            }, CreatePollyContext(cancellationToken), cancellationToken)).Result;
        }

        public async Task<Speaker?> CreateSpeakerAsync(Speaker speaker,
            CancellationToken cancellationToken = default)
        {
            this.RequireAuthorization();

            var policy = CreateHttpAsyncUnauthorizedPolicy<Speaker?>(() => null);
            return (await policy.ExecuteAsync(async (ctx, c) =>
            {
                var result = await SpeakerApi.CreateAsync(speaker, c);
                return (result, SpeakerApi.LastHttpStatusCode);
            }, CreatePollyContext(cancellationToken), cancellationToken)).Result;
        }

        public async Task<string?> UpdateSpeakerAsync(Speaker speaker,
            CancellationToken cancellationToken = default)
        {
            this.RequireAuthorization();

            var policy = CreateHttpAsyncUnauthorizedPolicy(() => false);
            bool result = (await policy.ExecuteAsync(async (ctx, c) =>
            {
                var r = await SpeakerApi.UpdateAsync(speaker, c);
                return (r, SpeakerApi.LastHttpStatusCode);
            }, CreatePollyContext(cancellationToken), cancellationToken)).Result;

            if (!result)
            {
                return SpeakerApi.LastError.Message;
            }
            return null;
        }

        public async Task<bool> DeleteSpeakerAsync(string dbId,
            CancellationToken cancellationToken = default)
        {
            this.RequireAuthorization();

            var policy = CreateHttpAsyncUnauthorizedPolicy(() => false);
            return (await policy.ExecuteAsync(async (ctx, c) =>
            {
                var result = await SpeakerApi.DeleteAsync(dbId, c);
                return (result, SpeakerApi.LastHttpStatusCode);
            }, CreatePollyContext(cancellationToken), cancellationToken)).Result;
        }
    }
}
