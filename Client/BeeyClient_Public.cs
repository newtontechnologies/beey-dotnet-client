using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Beey.Client
{
    public partial class BeeyClient
    {
        public Task<string> GetContentVersionAsync(CancellationToken cancellationToken = default)
            => LoginApi.GetContentVersionAsync(cancellationToken);

        public Task<JObject> GetPasswordSettingsAsync(CancellationToken cancellationToken = default)
            => LoginApi.GetPasswordSettingsAsync(cancellationToken);
    }
}
