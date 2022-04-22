using Beey.DataExchangeModel.Emails;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Beey.Api.Rest.Admin;

public class EmailApi : BaseAuthApi<EmailApi>
{
    public EmailApi(string url) : base(url)
    {
        EndPoint = "API/Admin/Email";
    }

    public async Task SendEmailAsync(PlainEmail email, CancellationToken cancellationToken)
    {
        var builder = CreateBuilder()
            .AddParameter("From", email.From)
            .AddParameter("Subject", email.Subject)
            .AddParameter("Body", email.Body);

        foreach (var to in email.To ?? Enumerable.Empty<string>())
            builder.AddParameter("To", to);

        foreach (var cc in email.CC ?? Enumerable.Empty<string>())
            builder.AddParameter("CC", cc);

        foreach (var bcc in email.BCC ?? Enumerable.Empty<string>())
            builder.AddParameter("BCC", bcc);
        
        var result = await builder.ExecuteAsync(HttpMethod.POST, cancellationToken);

        HandleResponse(result);
    }
}
