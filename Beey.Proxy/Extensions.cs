using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Text;

namespace Beey.Proxy
{
    public static class Extensions
    {
        public static IApplicationBuilder UseBeeyProxy(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<BeeyProxyMiddleware>();
        }

        public static IServiceCollection AddBeeyProxy(this IServiceCollection services)
        {
            services.AddSingleton<BeeyProxyAccessor>();
            services.AddTransient<BeeyProxy>(services => services.GetRequiredService<BeeyProxyAccessor>().BeeyData ?? throw new NullReferenceException("Beey data not set"));
            return services;
        }
    }
}
