using HotChocolate.Types;

namespace HotChocolate.Data.Filters.Expressions;

public class FilterVisitorTestBase
{
    protected ExecutorBuilder CreateProviderTester<TRuntimeType>(
        FilterInputType<TRuntimeType> type,
        FilterConvention? convention = null)
    {
        convention ??=
            new FilterConvention(
               x => x.AddDefaults().BindRuntimeType(typeof(TRuntimeType), type.GetType()));

        var builder = SchemaBuilder.New()
            .AddConvention<IFilterConvention>(convention)
            .TryAddTypeInterceptor<FilterTypeInterceptor>()
            .AddQueryType(c => c
                .Name("Query")
                .Field("foo")
                .Type<StringType>()
                .Resolve("bar"))
            .AddType(type)
            .AddType(new TimeSpanType(TimeSpanFormat.DotNet));

        builder.Create();

        return new ExecutorBuilder(type);
    }

    protected ISchema CreateSchema<T>(T type)
        where T : IFilterInputType
    {
        var convention = new FilterConvention(x => x.AddDefaults());
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
