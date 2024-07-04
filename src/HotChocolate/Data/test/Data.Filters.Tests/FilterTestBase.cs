using HotChocolate.Types;

namespace HotChocolate.Data.Tests;

public abstract class FilterTestBase
{
    public ISchema CreateSchema(Action<ISchemaBuilder> configure)
    {
        var builder = SchemaBuilder.New()
            .AddFiltering()
            .AddQueryType(c =>
                c.Name("Query")
                    .Field("foo")
                    .Type<StringType>()
                    .Resolve("bar"));

        configure(builder);

        return builder.Create();
    }
}
