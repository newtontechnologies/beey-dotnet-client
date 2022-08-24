using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

namespace Beey.Client;

public partial class BeeyClient
{
    public Task<string> GetContentVersionAsync(CancellationToken cancellationToken = default)
        => LoginApi.GetContentVersionAsync(cancellationToken);

    public Task<JsonObject> GetPasswordSettingsAsync(CancellationToken cancellationToken = default)
        => LoginApi.GetPasswordSettingsAsync(cancellationToken);
}
