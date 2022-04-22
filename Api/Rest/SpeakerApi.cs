using Beey.DataExchangeModel;
using Beey.DataExchangeModel.Auth;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using TranscriptionCore;

namespace Beey.Api.Rest;

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
        EndPoint = "XAPI/Speaker";
    }

    public async Task<Listing<Speaker>> ListAsync(int count, int skip, string? search,
        CancellationToken cancellationToken)
    {
        var bld = CreateBuilder()
            .AddUrlSegment("List")
            .AddParameter("skip", skip)
            .AddParameter("count", count);
        if (search != null)
            bld.AddParameter("search", search);

         var result = await bld.ExecuteAsync(HttpMethod.POST, cancellationToken);

        return HandleResponse(result, r => JsonConvert.DeserializeObject<Listing<Speaker>>(r.GetStringContent(), JsonConverters.Speaker));
    }

    public async Task<Speaker> GetAsync(string dbId, CancellationToken cancellationToken)
    {
        var result = await CreateBuilder()
            .AddUrlSegment(System.Web.HttpUtility.UrlEncode(dbId))
            .ExecuteAsync(HttpMethod.GET, cancellationToken);

        return HandleResponse(result, r => new Speaker(System.Xml.Linq.XElement.Parse(r.GetStringContent())));
    }

    public async Task<Speaker> CreateAsync(Speaker speaker,
        CancellationToken cancellationToken)
    {
        var result = await CreateBuilder()
            .SetBody(speaker.Serialize().ToString(), "text/xml")
            .ExecuteAsync(HttpMethod.POST, cancellationToken);

        return HandleResponse(result, r => new Speaker(System.Xml.Linq.XElement.Parse(r.GetStringContent())));
    }

    public async Task UpdateAsync(Speaker speaker,
        CancellationToken cancellationToken)
    {
        var result = await CreateBuilder()
            .SetBody(speaker.Serialize().ToString(), "text/xml")
            .ExecuteAsync(HttpMethod.PUT, cancellationToken);

        HandleResponse(result);
    }

    public async Task<bool> DeleteAsync(string dbId,
        CancellationToken cancellationToken)
    {
        var result = await CreateBuilder()
            .AddUrlSegment(System.Web.HttpUtility.UrlEncode(dbId))
            .ExecuteAsync(HttpMethod.DELETE, cancellationToken);

        if (ResultNotFound(result))
        {
            return false;
        }

        return HandleResponse(result, _ => true);
    }
}
