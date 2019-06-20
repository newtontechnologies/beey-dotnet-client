using Beey.DataExchangeModel;
using Beey.DataExchangeModel.Auth;
using Beey.DataExchangeModel.Emails;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Beey.Client
{
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

        public async Task<Listing<User>> ListUsersAsync(int count, int skip = 0,
            CancellationToken cancellationToken = default)
        {
            this.RequireAuthorization();

            var policy = CreateHttpAsyncUnauthorizedPolicy<Listing<User>>();
            return (await policy.ExecuteAsync(async (ctx, c) =>
            {
                return await UserApi.ListAsync(count, skip, c);
            }, CreatePollyContext(cancellationToken), cancellationToken));
        }

        public async Task<User> GetUserAsync(int id,
            CancellationToken cancellationToken = default)
        {
            this.RequireAuthorization();

            var policy = CreateHttpAsyncUnauthorizedPolicy<User>();
            return (await policy.ExecuteAsync(async (ctx, c) =>
            {
                return await UserApi.GetAsync(id, c);
            }, CreatePollyContext(cancellationToken), cancellationToken));
        }

        public async Task<User> CreateUserAsync(User User,
            CancellationToken cancellationToken = default)
        {
            this.RequireAuthorization();

            var policy = CreateHttpAsyncUnauthorizedPolicy<User>();
            return (await policy.ExecuteAsync(async (ctx, c) =>
            {
                return await UserApi.CreateAsync(User, c);
            }, CreatePollyContext(cancellationToken), cancellationToken));
        }

        public async Task UpdateUserAsync(User User,
            CancellationToken cancellationToken = default)
        {
            this.RequireAuthorization();

            var policy = CreateHttpAsyncUnauthorizedPolicy<bool>();
            await policy.ExecuteAsync(async (ctx, c) =>
            {
                await UserApi.UpdateAsync(User, c);
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
                return await UserApi.DeleteAsync(id, c);
            }, CreatePollyContext(cancellationToken), cancellationToken));
        }
    }
}
