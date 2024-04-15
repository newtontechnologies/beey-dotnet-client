using System.Threading;
using System.Threading.Tasks;
using Beey.Api.DTO;
using Beey.DataExchangeModel;
using Beey.DataExchangeModel.Auth;
using Beey.DataExchangeModel.Emails;

namespace Beey.Client;

public partial class BeeyClient
{
    public async Task SendEmailAsync(PlainEmail mail,
         CancellationToken cancellationToken = default)
    {
        this.RequireAuthorization();

        var policy = CreateHttpAsyncUnauthorizedPolicy<bool>();
        await policy.ExecuteAsync(async (ctx, c) =>
        {
            await EmailApi.SendEmailAsync(mail, c);
            return true;
        }, CreatePollyContext(cancellationToken), cancellationToken);
    }

    public async Task<Listing<UserDto>> ListUsersAsync(int count, int skip = 0,
        CancellationToken cancellationToken = default)
    {
        this.RequireAuthorization();

        var policy = CreateHttpAsyncUnauthorizedPolicy<Listing<UserDto>>();
        return (await policy.ExecuteAsync(async (ctx, c) =>
        {
            return await AdminUserApi.ListAsync(count, skip, c);
        }, CreatePollyContext(cancellationToken), cancellationToken));
    }

    public async Task<UserDto> GetUserAsync(int id,
        CancellationToken cancellationToken = default)
    {
        this.RequireAuthorization();

        var policy = CreateHttpAsyncUnauthorizedPolicy<UserDto>();
        return (await policy.ExecuteAsync(async (ctx, c) =>
        {
            return await AdminUserApi.GetAsync(id, c);
        }, CreatePollyContext(cancellationToken), cancellationToken));
    }

    public async Task<UserDto> CreateUserAsync(UserAddDto User,
        CancellationToken cancellationToken = default)
    {
        this.RequireAuthorization();

        var policy = CreateHttpAsyncUnauthorizedPolicy<UserDto>();
        return (await policy.ExecuteAsync(async (ctx, c) =>
        {
            return await AdminUserApi.CreateAsync(User, c);
        }, CreatePollyContext(cancellationToken), cancellationToken));
    }

    public async Task UpdateUserAsync(UserUpdateDto User,
        CancellationToken cancellationToken = default)
    {
        this.RequireAuthorization();

        var policy = CreateHttpAsyncUnauthorizedPolicy<bool>();
        await policy.ExecuteAsync(async (ctx, c) =>
        {
            await AdminUserApi.UpdateAsync(User, c);
            return true;
        }, CreatePollyContext(cancellationToken), cancellationToken);
    }

    public async Task<bool> DeleteUserAsync(int id,
        CancellationToken cancellationToken = default)
    {
        this.RequireAuthorization();

        var policy = CreateHttpAsyncUnauthorizedPolicy<bool>();
        return (await policy.ExecuteAsync(async (ctx, c) =>
        {
            return await AdminUserApi.DeleteAsync(id, c);
        }, CreatePollyContext(cancellationToken), cancellationToken));
    }

    public async Task<Listing<MonthlyTranscriptionLogItem>> GetTranscriptionLogAsync(int userId,
        CancellationToken cancellationToken = default)
    {
        this.RequireAuthorization();

        var policy = CreateHttpAsyncUnauthorizedPolicy<Listing<MonthlyTranscriptionLogItem>>();
        return (await policy.ExecuteAsync(async (ctx, c) =>
        {
            return await AdminUserApi.GetTranscriptionLogAsync(userId, c);
        }, CreatePollyContext(cancellationToken), cancellationToken));
    }
}
