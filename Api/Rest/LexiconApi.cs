using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Beey.DataExchangeModel.Lexicons;

namespace Beey.Api.Rest;

public class LexiconApi : BaseAuthApi<LexiconApi>
{
    public LexiconApi(string url) : base(url)
    {
        EndPoint = "XAPI/Lexicon";
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

        return HandleResponse(result, r => JsonSerializer.Deserialize<TmpValidationError[]>(r.GetStringContent(), new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
    }

    public async Task<TmpValidationError[]> ValidateLexiconAsync(IEnumerable<LexiconEntryDto> lexicon, string language,
        CancellationToken cancellationToken)
    {
        var result = await CreateBuilder()
            .AddUrlSegment("Validate")
            .AddParameter("language", language)
            .SetBody(JsonSerializer.Serialize(lexicon))
            .ExecuteAsync(HttpMethod.POST, cancellationToken);

        return HandleResponse(result, r => JsonSerializer.Deserialize<TmpValidationError[]>(r.GetStringContent(), new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
    }

    // TODO: remove when created in backend
    public class TmpValidationError
    {
        public string Text { get; set; }
        public string Pronunciation { get; set; }
        public string Error { get; set; }
    }
}
