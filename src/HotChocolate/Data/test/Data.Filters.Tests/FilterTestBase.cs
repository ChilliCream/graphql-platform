using System;
using HotChocolate.Data.Filters;
using HotChocolate.Types;

namespace HotChocolate.Data.Tests
{
    public abstract class FilterTestBase
    {
        public ISchema CreateSchema(Action<ISchemaBuilder> configure)
        {
            ISchemaBuilder builder = SchemaBuilder.New()
                .AddFiltering()
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
