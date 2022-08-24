using Beey.Api.Rest;
using Beey.DataExchangeModel.Auth;
using Beey.DataExchangeModel.Lexicons;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace XUnitTests;

[CollectionDefinition("6 - Lexicon Collection")]
public class C6_LexiconCollectionDefinition : ICollectionFixture<LoginFixture> { }


[Collection("6 - Lexicon Collection")]
public class C6_LexiconApiUnitTests
{
    private static LexiconApi api = new LexiconApi(Configuration.BeeyUrl);

    public C6_LexiconApiUnitTests(LoginFixture fixture)
    {
        api.Token = fixture.Token;
    }

    [Fact, TestPriority(1)]
    public async Task ValidateEntry()
    {
        var errors = await api.ValidateLexiconEntryAsync("spěch", "spěch", "cs-CZ", default);
        Assert.Single(errors);
        Assert.Equal("sp>ě<ch", errors[0].Error);
    }

    [Fact, TestPriority(2)]
    public async Task ValidateLexicon()
    {
        var errors = await api.ValidateLexiconAsync(new LexiconEntry[]
            {
                new LexiconEntry("spěch", "spěch"),
                new LexiconEntry("kouzelník", "kouzelník")
            },"cs-CZ", default);

        Assert.Equal(2, errors.Length);
        Assert.Equal("sp>ě<ch", errors.Single(e => e.Text == "spěch").Error);
        Assert.Equal("kouzeln>í<k", errors.Single(e => e.Text == "kouzelník").Error);
    }
}
