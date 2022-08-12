using System;
using HotChocolate.Data.Filters;
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

    public ISchema CreateSchemaWithFilter<T>(
        Action<IFilterInputTypeDescriptor<T>> configure,
        Action<ISchemaBuilder>? configureSchema = null)
    {
        var builder = SchemaBuilder.New()
            .AddFiltering()
            .ModifyOptions(x => x.RemoveUnreachableTypes = true)
            .AddQueryType(c =>
                c.Name("Query")
                    .Field("foo")
                    .Type<StringType>()
                    .UseFiltering(configure)
                    .Resolve("bar"));
        configureSchema?.Invoke(builder);

        return builder.Create();
    }
}
