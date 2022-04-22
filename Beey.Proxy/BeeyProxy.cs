using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Beey.Proxy;

public class BeeyProxy
{
    public BeeyProxy(string server, string integrationUrl, string authToken, int userid)
    {
        Server = server;
        IntegrationUrl = integrationUrl;
        AuthToken = authToken;
        UserId = userid;
    }

    public string Server { get; }
    public string IntegrationUrl { get; }
    public string AuthToken { get; }
    public int UserId { get; }
}
