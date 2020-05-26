using Beey.DataExchangeModel.Lexicons;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Beey.Api.Rest
{
    public class LexiconApi : BaseAuthApi<LexiconApi>
    {
        public LexiconApi(string url) : base(url)
        {
            EndPoint = "API/Lexicon";
        }

        public async Task<TmpValidationError[]> ValidateLexiconEntryAsync(string text, string pronunciation, string language,
            CancellationToken cancellationToken)
        {
            var result = await CreateBuilder()
                .AddUrlSegment("ValidateEntry")
                .AddParameter("text", text)
                .AddParameter("pronunciation", pronunciation)
                .AddParameter("language", language)
                .ExecuteAsync(HttpMethod.POST, cancellationToken);

            return HandleResponse(result, r => JsonConvert.DeserializeObject<TmpValidationError[]>(r.GetStringContent()));
        }

        public async Task<TmpValidationError[]> ValidateLexiconAsync(IEnumerable<LexiconEntry> lexicon, string language,
            CancellationToken cancellationToken)
        {
            var result = await CreateBuilder()
                .AddUrlSegment("Validate")
                .AddParameter("language", language)
                .SetBody(JsonConvert.SerializeObject(lexicon))
                .ExecuteAsync(HttpMethod.POST, cancellationToken);

            return HandleResponse(result, r => JsonConvert.DeserializeObject<TmpValidationError[]>(r.GetStringContent()));
        }

        // TODO: remove when created in backend
        public class TmpValidationError
        {
            public string Text { get; set; }
            public string Pronunciation { get; set; }
            public string Error { get; set; }
        }
    }
}
