﻿using System;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Beey.Api.Rest;
using Beey.DataExchangeModel.Lexicons;
using Xunit;

namespace XUnitTests;

[CollectionDefinition("2 - Current User Collection")]
public class C2_CurrentUserCollectionDefinition : ICollectionFixture<LoginFixture> { }

[Collection("2 - Current User Collection")]
public class C2_CurrentUserApiUnitTests
{
    private const string testPassword = "ASDF___ASDF";
    private static JsonObject? testSettings;
    private static readonly CurrentUserApi api = new CurrentUserApi(Configuration.BeeyUrl);

    public C2_CurrentUserApiUnitTests(LoginFixture fixture)
    {
        api.Token = fixture.Token;
    }

    [Fact, TestPriority(2)]
    public async Task ChangePasswordAsync()
    {
        await api.ChangePasswordAsync(Configuration.Password, testPassword, default);
        await api.ChangePasswordAsync(testPassword, Configuration.Password, default);
    }

    [Fact, TestPriority(3)]
    public async Task PostUserSettingsAsync()
    {
        testSettings = (JsonObject)JsonNode.Parse($"{{ \"Name\": \"Value_{DateTime.Now}\" }}")!;
        await api.PostUserSettingsAsync(testSettings!, default);
    }

    [Fact, TestPriority(4)]
    public async Task GetUserSettings()
    {
        var jSettings = await api.GetUserSettingsAsync(default);

        Assert.Equal(testSettings!.ToString(), jSettings.ToString());
    }

    [Fact, TestPriority(5)]
    public async Task GetUserInfo()
    {
        var info = await api.GetUserInfoAsync(default);
        Assert.NotNull(info);
    }

    [Fact, TestPriority(6)]
    public async Task GetTranscriptionLog()
    {
        await api.GetTranscriptionLogAsync(default);
    }

    [Fact, TestPriority(7)]
    public async Task SetUserLex()
    {
        await api.SetUserLexAsync("cs-CZ", new LexiconEntryDto[]
            {
                new LexiconEntryDto("test", "test")
            }, default);
    }

    [Fact, TestPriority(8)]
    public async Task GetUserLex()
    {
        var userLex = await api.GetUserLexAsync("cs-CZ", default);
        Assert.Single(userLex);
        Assert.Equal("test", userLex[0].Text);
        Assert.Equal("test", userLex[0].IncorrectTranscription);
    }

    [Fact, TestPriority(9)]
    public async Task GetUserMessages()
    {
        await api.GetUserMessagesAsync(null, default);
    }
}
