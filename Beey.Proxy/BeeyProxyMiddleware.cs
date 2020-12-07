using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Beey.Proxy
{
    internal class BeeyProxyMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly BeeyProxyAccessor accessor;

        public BeeyProxyMiddleware(RequestDelegate next, BeeyProxyAccessor accessor)
        {
            _next = next;
            this.accessor = accessor;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (context.Request.Headers.TryGetValue($"Beey-Server", out var bs)
            && context.Request.Headers.TryGetValue($"Beey-IntegrationUrl", out var bi)
            && context.Request.Headers.TryGetValue($"Beey-Authorization", out var ba)
             && context.Request.Headers.TryGetValue($"Beey-UserID", out var bu) && int.TryParse(bu.First(), out var uid))
            {
                var bdata = new BeeyProxy(bs.First(), bi.First(), ba.First(), uid);
                accessor.BeeyData = bdata;
            }

            await _next(context);
        }
    }
}
