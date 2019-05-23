using BeeyApi.POCO;
using BeeyApi.POCO.Auth;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using TranscriptionCore;

namespace BeeyApi.Rest
{
    /// <summary>
    /// All methods:
    /// 1) returns result if everything is ok,
    /// 2) throws exception when there is some network or framework problem,
    /// 3a) throws exception when server returned 500
    /// 3b) returns null if everything is ok, but backend returned error.
    ///     Description of the error is then in the properties LastError and LastHttpStatusCode.
    /// </summary>
    public class SpeakerApi : BaseAuthApi<SpeakerApi>
    {
        public SpeakerApi(string url) : base(url)
        {
            EndPoint = "API/Speaker/";
        }

        public async Task<Listing<Speaker>?> ListAsync(int? count, int? skip, string? search, CancellationToken cancellationToken)
        {
            var result = await CreateBuilder()
                .AddUrlSegment("List")
                .AddParameters(new { skip = skip?.ToString(), count = count?.ToString(), search })
                .ExecuteAsync(HttpMethod.POST, cancellationToken);

            return HandleResponse(result, HttpStatusCode.OK,
                r => JsonConvert.DeserializeObject<Listing<Speaker>?>(r.GetStringContent(), JsonConverters.Speaker),
                _ => null);
        }

        public async Task<Speaker?> GetAsync(string dbId, CancellationToken cancellationToken)
        {
            var result = await CreateBuilder()
                .AddParameter("id", dbId)
                .ExecuteAsync(HttpMethod.GET, cancellationToken);

            return HandleResponse<Speaker?>(result, HttpStatusCode.OK,
                r => new Speaker(System.Xml.Linq.XElement.Parse(r.GetStringContent())),
                _ => null);
        }

        public async Task<Speaker?> CreateAsync(Speaker speaker, CancellationToken cancellationToken)
        {
            var result = await CreateBuilder()
                .SetBody(speaker.Serialize().ToString(), "text/xml")
                .ExecuteAsync(HttpMethod.POST, cancellationToken);

            return HandleResponse<Speaker?>(result, HttpStatusCode.OK,
                r => new Speaker(System.Xml.Linq.XElement.Parse(r.GetStringContent())),
                _ => null);
        }

        public async Task<bool> UpdateAsync(Speaker speaker, CancellationToken cancellationToken)
        {
            var result = await CreateBuilder()
                .SetBody(speaker.Serialize().ToString(), "text/xml")
                .ExecuteAsync(HttpMethod.PUT, cancellationToken);

            return HandleResponse(result, HttpStatusCode.OK,
                _ => true,
                _ => false);
        }

        public async Task<bool> DeleteAsync(string dbId, CancellationToken cancellationToken)
        {
            var result = await CreateBuilder()
                .AddParameter("id", dbId)
                .ExecuteAsync(HttpMethod.DELETE, cancellationToken);

            return HandleResponse(result, HttpStatusCode.OK,
                _ => true,
                _ => false);
        }
    }
}
