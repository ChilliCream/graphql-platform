using System;
using HotChocolate;
using HotChocolate.Types;
using MarshmallowPie.GraphQL.Clients;
using MarshmallowPie.GraphQL.Environments;
using MarshmallowPie.GraphQL.Schemas;

namespace MarshmallowPie.GraphQL
{
    public static class RegistrySchemaExtensions
    {
        public static ISchemaBuilder AddSchemaRegistry(
            this ISchemaBuilder builder,
            bool addRootTypes = true)
        {
            if (addRootTypes)
            {
                builder
                    .AddQueryType(d => d.Name("Query"))
                    .AddMutationType(d => d.Name("Mutation"))
                    .AddSubscriptionType(d => d.Name("Subscription"));
            }

            return builder
                .AddType<EnvironmentQueries>()
                .AddType<EnvironmentMutations>()
                .AddType<SchemaQueries>()
                .AddType<SchemaMutations>()
                .AddType<SchemaSubscriptions>()
                .AddType<SchemaExtension>()
                .AddType<SchemaVersionExtension>()
                .AddType<SchemaPublishReportExtension>()
                .AddType<ClientQueries>()
                .AddType<ClientMutations>()
                .AddType<ClientExtension>()
                .AddType<ClientVersionExtension>()
                .BindClrType<string, StringType>()
                .BindClrType<Guid, IdType>();
        }
    }
}
