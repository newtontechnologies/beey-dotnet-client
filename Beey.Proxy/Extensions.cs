using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Primitives;

namespace Beey.Proxy;

public static class Extensions
{
    /// <summary>
    ///
    /// </summary>
    /// <param name="builder"></param>
    /// <param name="injectProxyHeaders">simulate </param>
    /// <returns></returns>
    public static IApplicationBuilder UseBeeyProxy(this IApplicationBuilder builder, bool injectProxyHeaders)
    {
        var bldr = builder.UseMiddleware<BeeyProxyMiddleware>();

        if (injectProxyHeaders)
        {
            bldr = bldr.Use((context, next) =>
            {
                Uri? url = null;

                if (context.Request.Headers.TryGetValue("Beey-IntegrationUrl", out var value))
                {
                    url = new Uri(value!)!;

                    //    var holder = context.RequestServices.GetService<BeeyProxyHolder>();
                    //context.Request.PathBase = ;

                    if (!context.Request.Headers.TryGetValue(ForwardedHeadersDefaults.XForwardedProtoHeaderName, out var proto) || proto != url.Scheme)
                        context.Request.Headers.Append(ForwardedHeadersDefaults.XForwardedProtoHeaderName, url.Scheme);

                    if (!context.Request.Headers.TryGetValue(ForwardedHeadersDefaults.XForwardedProtoHeaderName, out var host) || host != url.Authority)
                        context.Request.Headers.Append(ForwardedHeadersDefaults.XForwardedHostHeaderName, url.Authority);

                    if (!context.Request.Headers.TryGetValue(ForwardedHeadersDefaults.XForwardedPrefixHeaderName, out var prefix) || host != url.AbsolutePath)
                        context.Request.Headers.Append(ForwardedHeadersDefaults.XForwardedPrefixHeaderName, url.AbsolutePath);
                }
                var res = next(context);

                if (url is { } && context.Response.Headers.Location != StringValues.Empty)
                {
                    //fixup location to relative path for Beey versions prior 1.6

                    var loc = context.Response.Headers.Location.ToString();

                    if (loc.StartsWith(url.ToString()))
                    {
                        context.Response.Headers.Location = loc.Substring(url.ToString().Length);
                    }
                    else if (loc.StartsWith(url.AbsolutePath.ToString()))
                    {
                        context.Response.Headers.Location = loc.Substring(url.AbsolutePath.ToString().Length);
                    }
                }

                return res;
            });
        }

        return bldr;
    }

    public static IServiceCollection AddBeeyProxy(this IServiceCollection services)
    {
        services.AddSingleton<BeeyProxyAccessor>();
        services.AddTransient<BeeyProxyHolder>(services => services.GetRequiredService<BeeyProxyAccessor>().BeeyDataHolder);
        services.AddTransient<BeeyProxy>(services => services.GetRequiredService<BeeyProxyAccessor>().BeeyData ?? throw new NullReferenceException("Beey data not set"));
        return services;
    }
}
