using Beey.DataExchangeModel.Auth;
using Beey.DataExchangeModel.Lexicons;
using Beey.DataExchangeModel.Messaging;
using Beey.DataExchangeModel.Projects;
using Beey.DataExchangeModel.Serialization.JsonConverters;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Beey.Api.Rest
{
    public class CurrentUserApi : BaseAuthApi<CurrentUserApi>
    {
        public CurrentUserApi(string url) : base(url)
        {
            EndPoint = "API/CurrentUser";
        }

        public async Task ChangePasswordAsync(string oldPassword, string newPassword,
            CancellationToken cancellationToken)
        {
            var result = await CreateBuilder()
                .AddUrlSegment("ChangePassword")
                .AddParameters(("password", oldPassword), ("newPassword", newPassword))
                .ExecuteAsync(HttpMethod.POST, cancellationToken);

            HandleResponse(result);
        }

        public async Task<LoginToken> GetUserInfoAsync(CancellationToken cancellationToken)
        {
            var result = await CreateBuilder()
                .ExecuteAsync(HttpMethod.GET, cancellationToken);

            return HandleResponse(result, r => JsonConvert.DeserializeObject<LoginToken>(r.GetStringContent()));
        }


        public async Task<JObject> GetUserSettingsAsync(CancellationToken cancellationToken)
        {
            var result = await CreateBuilder()
                .AddUrlSegment("Settings")
                .ExecuteAsync(HttpMethod.GET, cancellationToken);

            return HandleResponse(result, r => JObject.Parse(r.GetStringContent()));
        }

        public async Task PostUserSettingsAsync(JObject settings,
            CancellationToken cancellationToken)
        {
            var result = await CreateBuilder()
                .AddUrlSegment("Settings")
                .SetBody(settings.ToString())
                .ExecuteAsync(HttpMethod.POST, cancellationToken);

            HandleResponse(result);
        }

        public async Task<TranscriptionLogItem[]> GetTranscriptionLogAsync(CancellationToken cancellationToken)
        {
            var result = await CreateBuilder()
                .AddUrlSegment("TranscriptionLog")
                .ExecuteAsync(HttpMethod.GET, cancellationToken);

            return HandleResponse(result, r => JsonConvert.DeserializeObject<TranscriptionLogItem[]>(r.GetStringContent()));
        }

        public async Task<LexiconEntry[]> GetUserLexAsync(string language, CancellationToken cancellationToken)
        {
            var result = await CreateBuilder()
                .AddUrlSegment("Lexicon")
                .AddParameter("language", language)
                .ExecuteAsync(HttpMethod.GET, cancellationToken);

            return HandleResponse(result, r => JsonConvert.DeserializeObject<LexiconEntry[]>(r.GetStringContent()));
        }

        public async Task SetUserLexAsync(string language, IEnumerable<LexiconEntry> userLex,
            CancellationToken cancellationToken)
        {
            var result = await CreateBuilder()
                .AddUrlSegment("Lexicon")
                .AddParameter("language", language)
                .SetBody(JsonConvert.SerializeObject(userLex), "application/json")
                .ExecuteAsync(HttpMethod.POST, cancellationToken);

            HandleResponse(result);
        }

        public async Task<MessageNew[]> GetUserMessagesAsync(DateTime? from, CancellationToken cancellationToken)
        {
            var result = await CreateBuilder()
                .AddUrlSegment("MessageCache")
                .AddParameter("from", from)
                .ExecuteAsync(HttpMethod.GET, cancellationToken);

            return HandleResponse(result, r => System.Text.Json.JsonSerializer.Deserialize<MessageNew[]>(r.GetStringContent(), GetJsonSerializerOptions()));
        }

        private static JsonSerializerOptions GetJsonSerializerOptions()
            => new JsonSerializerOptions().AddConverters(new JsonMessageConverter());
    }
}
