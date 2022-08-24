using Beey.Api.Rest;
using Beey.DataExchangeModel.Auth;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace XUnitTests;

[CollectionDefinition("1 - Login Collection")]
public class C1_LoginCollectionDefinition { }


[Collection("1 - Login Collection")]
public class C1_LoginApiUnitTests
{
    private static LoginApi? api;
    private static LoginToken? token;

    [Fact, TestPriority(1)]
    public async Task LoginAsync()
    {
        api = new LoginApi(Configuration.BeeyUrl);
        token = await api.LoginAsync(Configuration.Email, Configuration.Password, default);
    }

    [Fact, TestPriority(2)]
    public async Task LogoutAsync()
    {
        await api!.LogoutAsync(token!, default);
    }

    [Fact, TestPriority(3)]
    public async Task GetVersionAsync()
    {
        await api!.GetContentVersionAsync(default);
    }

    [Fact, TestPriority(4)]
    public async Task GetPasswordSettingsAsync()
    {
        await api!.GetPasswordSettingsAsync(default);
    }
}
