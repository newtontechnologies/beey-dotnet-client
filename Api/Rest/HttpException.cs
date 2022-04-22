using System;
using System.Collections.Generic;
using System.Net;
using System.Text;

namespace Beey.Api.Rest;

public class HttpException : Exception
{
    public HttpStatusCode HttpStatusCode { get; private set; }

    public HttpException(string message, HttpStatusCode statusCode)
        : base(message)
    {
        HttpStatusCode = statusCode;
    }
}
