using System;
using HotChocolate.Data.Sorting;
using HotChocolate.Types;

namespace HotChocolate.Data.Tests;

public abstract class SortTestBase
{
    public ISchema CreateSchema(Action<ISchemaBuilder> configure)
    {
        var builder = SchemaBuilder.New()
            .AddSorting()
            .AddQueryType(c =>
                c.Name("Query")
                    .Field("foo")
                    .Type<StringType>()
                    .Resolve("bar"));

        configure(builder);

        return builder.Create();
    }

    public ISchema CreateSchemaWithSort<T>(
        Action<ISortInputTypeDescriptor<T>> configure,
        Action<ISchemaBuilder>? configureSchema = null)
    {
        var builder = SchemaBuilder.New()
            .AddSorting()
            .ModifyOptions(x => x.RemoveUnreachableTypes = true)
            .AddQueryType(c =>
                c.Name("Query")
                    .Field("foo")
                    .Type<StringType>()
                    .UseSorting(configure)
                    .Resolve("bar"));
        configureSchema?.Invoke(builder);

        return builder.Create();
    }
}
