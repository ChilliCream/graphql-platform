using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HotChocolate.Stitching.Schemas.Accounts
{
    public static class AccountsSchemaRequestExecutorBuilderExtensions
    {
        public static IRequestExecutorBuilder AddAccountsSchema(
            this IRequestExecutorBuilder builder)
        {
            builder.Services
                .AddSingleton<UserRepository>();

            return builder
                .AddQueryType<Query>();
        }
    }
}
