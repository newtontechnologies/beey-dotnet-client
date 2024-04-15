using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Beey.DataExchangeModel;
using Beey.DataExchangeModel.Orders;

namespace Beey.Api.Rest;

public class OrderApi : BaseAuthApi<OrderApi>
{
    public OrderApi(string url) : base(url)
    {
        EndPoint = "XAPI/Orders";
    }

    public async Task<string> CreateCreditOrderAsync(uint credit, CancellationToken cancellationToken)
    {
        var result = await CreateBuilder()
            .AddUrlSegment("Create")
            .AddParameter("credit", credit)
            .ExecuteAsync(HttpMethod.GET, cancellationToken);

        return HandleResponse(result, r => r.GetStringContent());
    }

    public async Task<Listing<OrderInfoDto>> ListOrders(CancellationToken cancellationToken)
    {
        var result = await CreateBuilder()
            .AddUrlSegment("List")
            .ExecuteAsync(HttpMethod.GET, cancellationToken);

        return HandleResponse(result, r => JsonSerializer.Deserialize<Listing<OrderInfoDto>>(r.GetStringContent()));
    }
}
