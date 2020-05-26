using Beey.DataExchangeModel.Auth;
using Beey.DataExchangeModel.Lexicons;
using Beey.DataExchangeModel.Messaging;
using Beey.DataExchangeModel.Projects;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Beey.Client
{
    public partial class BeeyClient
    {
        public async Task ChangePasswordAsync(string oldPassword, string newPassword,
            CancellationToken cancellationToken = default)
        {
            this.RequireAuthorization();

            var policy = CreateHttpAsyncUnauthorizedPolicy<bool>();
            await policy.ExecuteAsync(async (ctx, c) =>
            {
                await CurrentUserApi.ChangePasswordAsync(oldPassword, newPassword, cancellationToken);
                return true;
            }, CreatePollyContext(cancellationToken), cancellationToken);
        }

        public async Task<LoginToken> GetUserInfoAsync(CancellationToken cancellationToken = default)
        {
            this.RequireAuthorization();

            var policy = CreateHttpAsyncUnauthorizedPolicy<LoginToken>();
            return await policy.ExecuteAsync(async (ctx, c) =>
            {
                return await CurrentUserApi.GetUserInfoAsync(cancellationToken);
            }, CreatePollyContext(cancellationToken), cancellationToken);
        }

        public async Task<JObject> GetUserSettingsAsync(CancellationToken cancellationToken = default)
        {
            this.RequireAuthorization();

            var policy = CreateHttpAsyncUnauthorizedPolicy<JObject>();
            return await policy.ExecuteAsync(async (ctx, c) =>
            {
                return await CurrentUserApi.GetUserSettingsAsync(cancellationToken);
            }, CreatePollyContext(cancellationToken), cancellationToken);
        }

        public async Task PostUserSettingsAsync(string settings,
            CancellationToken cancellationToken = default)
        {
            JObject jSettings = JObject.Parse(settings);
            await PostUserSettingsAsync(jSettings, cancellationToken);
        }
        public async Task PostUserSettingsAsync(JObject settings,
            CancellationToken cancellationToken = default)
        {
            this.RequireAuthorization();

            var policy = CreateHttpAsyncUnauthorizedPolicy<bool>();
            await policy.ExecuteAsync(async (ctx, c) =>
            {
                await CurrentUserApi.PostUserSettingsAsync(settings, cancellationToken);
                return true;
            }, CreatePollyContext(cancellationToken), cancellationToken);
        }

        public async Task<LexiconEntry[]> GetUserLexAsync(string language, CancellationToken cancellationToken = default)
        {
            this.RequireAuthorization();

            var policy = CreateHttpAsyncUnauthorizedPolicy<LexiconEntry[]>();
            return await policy.ExecuteAsync(async (ctx, c) =>
            {
                return await CurrentUserApi.GetUserLexAsync(language, cancellationToken);
            }, CreatePollyContext(cancellationToken), cancellationToken);
        }

        public async Task SetUserLexAsync(string language, IEnumerable<LexiconEntry> userLex, CancellationToken cancellationToken = default)
        {
            this.RequireAuthorization();

            var policy = CreateHttpAsyncUnauthorizedPolicy<bool>();
            await policy.ExecuteAsync(async (ctx, c) =>
            {
                await CurrentUserApi.SetUserLexAsync(language, userLex, cancellationToken);
                return true;
            }, CreatePollyContext(cancellationToken), cancellationToken);
        }

        public async Task<Object[]> GetUserMessagesAsync(DateTime? from, CancellationToken cancellationToken = default)
        {
            this.RequireAuthorization();

            var policy = CreateHttpAsyncUnauthorizedPolicy<JObject[]>();
            return await policy.ExecuteAsync(async (ctx, c) =>
            {
                return await CurrentUserApi.GetUserMessagesAsync(from, cancellationToken);
            }, CreatePollyContext(cancellationToken), cancellationToken);
        }

        public async Task<TranscriptionLogItem[]> GetTranscriptionLogAsync(CancellationToken cancellationToken = default)
        {
            this.RequireAuthorization();

            var policy = CreateHttpAsyncUnauthorizedPolicy<TranscriptionLogItem[]>();
            return await policy.ExecuteAsync(async (ctx, c) =>
            {
                return await CurrentUserApi.GetTranscriptionLogAsync(cancellationToken);
            }, CreatePollyContext(cancellationToken), cancellationToken);
        }
    }
}
