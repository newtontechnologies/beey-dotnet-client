using Beey.Api.Rest.Admin;
using Beey.Client;
using Beey.DataExchangeModel.Emails;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace XUnitTests
{
    [CollectionDefinition("4 - Email Collection")]
    public class EmailCollectionDefinition : ICollectionFixture<LoginFixture> { }

    [Collection("4 - Email Collection")]
    public class EmailApiTests
    {
        static readonly EmailApi api = new EmailApi(Configuration.BeeyUrl);

        public EmailApiTests(LoginFixture fixture)
        {
            api.Token = fixture.Token;
        }

        [Fact, TestPriority(1)]
        public async Task SendMail()
        {
            var mail = new PlainEmail();
            mail.To = new string[] { "milos.kudelka@newtontech.cz" };
            mail.Subject = "TEST";
            mail.Body = "This is only test email. Sorry. <br/><br/>Beey";

            await api.SendEmailAsync(mail, default);
        }
    }
}
