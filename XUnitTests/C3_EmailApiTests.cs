using Beey.Api.Rest.Admin;
using Beey.Client;
using Beey.DataExchangeModel.Emails;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace XUnitTests;

[CollectionDefinition("3 - Email Collection")]
public class C3_EmailCollectionDefinition : ICollectionFixture<LoginFixture> { }

[Collection("3 - Email Collection")]
public class C3_EmailApiTests
{
    static readonly EmailApi api = new EmailApi(Configuration.BeeyUrl);

    public C3_EmailApiTests(LoginFixture fixture)
    {
        api.Token = fixture.Token;
    }

    [Fact, TestPriority(1)]
    public async Task SendMailHtml()
    {
        var mail = new PlainEmail();
        mail.To = new string[] { "milos.kudelka@newtontech.cz" };
        mail.Subject = "TEST";
        mail.Body = "<html><body>Test - html email.<br/><br/>This sentence should be on a separate line.<br/><br/>Beey</body></html>";

        await api.SendEmailAsync(mail, default);
    }

    [Fact, TestPriority(2)]
    public async Task SendMailPlainText()
    {
        var mail = new PlainEmail();
        mail.To = new string[] { "milos.kudelka@newtontech.cz" };
        mail.Subject = "TEST";
        mail.Body = @"
Test - plain text email.

This sentence should be on a separate line.

Beey";

        await api.SendEmailAsync(mail, default);
    }
}
