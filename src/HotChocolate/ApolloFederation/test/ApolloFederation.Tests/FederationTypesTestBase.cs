using System;
using HotChocolate.Types;
using Xunit;

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

        protected void AssertDirectiveHasFieldsArgument(DirectiveType directive)
        {
            Assert.Collection(directive.Arguments,
                t =>
                {
                    Assert.Equal("fields", t.Name);
                    Assert.IsType<FieldSetType>(
                        Assert.IsType<NonNullType>(t.Type).Type);
                }
            );
        }
    }
}