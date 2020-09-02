using Beey.DataExchangeModel;
using Beey.DataExchangeModel.Orders;
using Beey.DataExchangeModel.Projects;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Beey.Api.Rest
{
    public class OrderApi : BaseAuthApi<OrderApi>
    {
        public OrderApi(string url) : base(url)
        {
            EndPoint = "API/Orders";
        }

        public async Task<string> CreateCreditOrderAsync(uint credit, CancellationToken cancellationToken)
        {
            var result = await CreateBuilder()
                .AddUrlSegment("Create")
                .AddParameter("credit", credit)
                .ExecuteAsync(HttpMethod.GET, cancellationToken);

            return HandleResponse(result, r => r.GetStringContent());
        }

        public async Task<Listing<OrderInfo>> ListOrders(CancellationToken cancellationToken)
        {
            var result = await CreateBuilder()
                .AddUrlSegment("List")
                .ExecuteAsync(HttpMethod.GET, cancellationToken);

            return HandleResponse(result, r => JsonConvert.DeserializeObject<Listing<OrderInfo>>(r.GetStringContent()));
        }
    }
}
