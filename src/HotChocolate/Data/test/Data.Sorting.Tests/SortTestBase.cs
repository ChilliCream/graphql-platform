using System;
using HotChocolate.Data.Sorting;
using HotChocolate.Types;

namespace HotChocolate.Data.Tests
{
    public abstract class SortTestBase
    {
        public ISchema CreateSchema(Action<ISchemaBuilder> configure)
        {
            ISchemaBuilder builder = SchemaBuilder.New()
                .AddSorting()
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
