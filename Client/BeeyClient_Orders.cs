using System.Threading;
using System.Threading.Tasks;
using Beey.DataExchangeModel;
using Beey.DataExchangeModel.Orders;

namespace Beey.Client;

public partial class BeeyClient
{
    public async Task<string> CreateCreditOrderAsync(uint credit, CancellationToken cancellationToken = default)
    {
        this.RequireAuthorization();

        var policy = CreateHttpAsyncUnauthorizedPolicy<string>();
        return await policy.ExecuteAsync(async (ctx, c) =>
        {
            return await OrderApi.CreateCreditOrderAsync(credit, cancellationToken);
        }, CreatePollyContext(cancellationToken), cancellationToken);
    }

    public async Task<Listing<OrderInfoDto>> ListOrdersAsync(CancellationToken cancellationToken = default)
    {
        this.RequireAuthorization();

        var policy = CreateHttpAsyncUnauthorizedPolicy<Listing<OrderInfoDto>>();
        return await policy.ExecuteAsync(async (ctx, c) =>
        {
            return await OrderApi.ListOrders(cancellationToken);
        }, CreatePollyContext(cancellationToken), cancellationToken);
    }
}
