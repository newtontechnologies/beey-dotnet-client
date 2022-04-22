using Beey.DataExchangeModel;
using Beey.DataExchangeModel.Auth;
using Beey.DataExchangeModel.Lexicons;
using Beey.DataExchangeModel.Messaging;
using Beey.DataExchangeModel.Orders;
using Beey.DataExchangeModel.Projects;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

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

    public async Task<Listing<OrderInfoViewModel>> ListOrdersAsync(CancellationToken cancellationToken = default)
    {
        this.RequireAuthorization();

        var policy = CreateHttpAsyncUnauthorizedPolicy<Listing<OrderInfoViewModel>>();
        return await policy.ExecuteAsync(async (ctx, c) =>
        {
            return await OrderApi.ListOrders(cancellationToken);
        }, CreatePollyContext(cancellationToken), cancellationToken);
    }
}
