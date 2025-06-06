using HotChocolate.Data.Filters.Expressions;
using HotChocolate.Types;

namespace HotChocolate.Data.Filters.Spatial.Expressions;

public class FilterVisitorTestBase
{
    protected ExecutorBuilder CreateProviderTester<TRuntimeType>(
        FilterInputType<TRuntimeType> type,
        FilterConvention? convention = null)
    {
        convention ??= new FilterConvention(x => x
            .AddDefaults()
            .AddSpatialOperations()
            .BindSpatialTypes()
            .Provider(
                new QueryableFilterProvider(
                    p => p.AddSpatialHandlers().AddDefaultFieldHandlers())));

        var builder = SchemaBuilder.New()
            .AddConvention<IFilterConvention>(convention)
            .TryAddTypeInterceptor<FilterTypeInterceptor>()
            .AddQueryType(c => c
                .Name("Query")
                .Field("foo")
                .Type<StringType>()
                .Resolve("bar"))
            .AddType(type);

        builder.Create();

        return new ExecutorBuilder(type);
    }

    protected Schema CreateSchema<T>(T type)
        where T : IFilterInputType
    {
        var convention = new FilterConvention(x => x
            .AddDefaults()
            .AddSpatialOperations()
            .BindSpatialTypes()
            .Provider(
                new QueryableFilterProvider(
                    p => p.AddSpatialHandlers().AddDefaultFieldHandlers())));

        var builder = SchemaBuilder.New()
            .AddConvention<IFilterConvention>(convention)
            .TryAddTypeInterceptor<FilterTypeInterceptor>()
            .AddQueryType(c => c
                .Name("Query")
                .Field("foo")
                .Type<StringType>()
                .Resolve("bar"))
            .AddType(type);

        return builder.Create();
    }
}
