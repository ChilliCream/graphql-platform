using HotChocolate.Data.Filters.Expressions;
using HotChocolate.Types;

namespace HotChocolate.Data.Filters.Spatial.Tests;

public abstract class FilterTestBase
{
    public ISchema CreateSchema(Action<ISchemaBuilder> configure)
    {
        var builder = SchemaBuilder.New()
            .AddFiltering(x => x
                .AddDefaults()
                .AddSpatialOperations()
                .BindSpatialTypes()
                .Provider(
                    new QueryableFilterProvider(
                        p => p.AddSpatialHandlers().AddDefaultFieldHandlers())))
            .AddQueryType(c => c
                .Name("Query")
                .Field("foo")
                .Type<StringType>()
                .Resolve("bar"));

        configure(builder);

        return builder.Create();
    }
}
