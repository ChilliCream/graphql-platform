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
                .AddGraphQLServer()
                .AddQueryType<Query>()
                .PublishSchemaDefinition(c => c
                    .SetName("accounts")
                    .IgnoreRootTypes()
                    .AddTypeExtensionsFromString(
                        @"extend type Query {
                             me: User! @delegate(path: ""user(id: 1)"")
                        }

                        extend type Review {
                            author: User @delegate(path: ""user(id: $fields:authorId)"")
                        }"));
        }
    }
}
