using System;
using HotChocolate.Types;

namespace HotChocolate.ApolloFederation
{
    public class FederationTypesTestBase
    {
        protected ISchema CreateSchema(Action<ISchemaBuilder> configure)
        {
            ISchemaBuilder builder = SchemaBuilder.New()
                .AddQueryType(c =>
                    c.Name("Query")
                        .Field("foo")
                        .Type<StringType>()
                        .Resolver("bar"));

            configure(builder);

            return builder.Create();
        }
    }
}