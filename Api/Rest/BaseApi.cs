﻿using Beey.DataExchangeModel;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Beey.Api.Rest;

public abstract class BaseApi<TApi> where TApi : BaseApi<TApi>
{
    public static Error NoError { get; } = new Error("OK", true);

    protected readonly ILogger<TApi> Logger = LoggerFactoryProvider.LoggerFactory.CreateLogger<TApi>();

    protected string Url { get; set; }
    protected string? EndPoint { get; set; }

    public object? LastErrorData { get; private set; }

    protected string GetServerErrorMessage(string content)
    {
        string errorMessage = "";
        LastErrorData = null;
        try
        {
            var error = JsonSerializer.Deserialize<Error>(content);
            LastErrorData = error?.Data;
            errorMessage = error?.Message ?? content;
        }
        catch (Exception)
        {
            errorMessage = content;
        }

        return errorMessage;
    }

    public BaseApi(string url)
    {
        Url = url;
    }

    internal virtual RestRequestBuilder CreateBuilder()
    {
        return new RestRequestBuilder(this.Url).AddUrlSegment(EndPoint);
    }

    internal void HandleResponse(Response response)
    {
        _ = HandleResponse(response, r => new object());
    }

    internal T HandleResponse<T>(Response response, Func<Response, T> getValue)
    {
        if (response.IsSuccessStatusCode == false)
        {
            string serverError = GetServerErrorMessage(response.GetStringContent());
            string errMsg = $"Server error: {response.StatusCode.ToString()} ({(int)response.StatusCode}){Environment.NewLine}{serverError}";

            Logger.LogError(errMsg);
            if (response.StatusCode == HttpStatusCode.Unauthorized
                || response.StatusCode == HttpStatusCode.Forbidden)
            {
                throw new UnauthorizedAccessException(errMsg);
            }
            else
            {
                throw new HttpException(errMsg, response.StatusCode);
            }
        }

        return getValue(response);
    }

    internal async Task HandleResponseAsync(Response response, CancellationToken cancellationToken)
    {
        var _ = await HandleResponseAsync(response, (r, c) => Task.FromResult(new object()), cancellationToken);
    }

    internal async Task<T> HandleResponseAsync<T>(Response response, Func<Response, CancellationToken, Task<T>> getValue,
    CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode == false)
        {
            string serverError = GetServerErrorMessage(response.GetStringContent()); ;
            string errMsg = $"Server error: {response.StatusCode.ToString()}({(int)response.StatusCode}){Environment.NewLine}{serverError}";

            Logger.LogError(errMsg);
            if (response.StatusCode == HttpStatusCode.Unauthorized
                || response.StatusCode == HttpStatusCode.Forbidden)
            {
                throw new UnauthorizedAccessException(errMsg);
            }
            else
            {
                throw new HttpException(errMsg, response.StatusCode);
            }
        }

        return await getValue(response, cancellationToken);
    }

    internal bool ResultNotFound(Response response)
    {
        return response.StatusCode == HttpStatusCode.NotFound;
    }
}
