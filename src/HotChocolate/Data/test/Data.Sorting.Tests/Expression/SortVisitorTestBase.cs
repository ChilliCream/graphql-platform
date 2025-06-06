using HotChocolate.Types;

namespace HotChocolate.Data.Sorting.Expressions;

public class SortVisitorTestBase
{
    protected ExecutorBuilder CreateProviderTester<TRuntimeType>(
        SortInputType<TRuntimeType> type,
        SortConvention? convention = null)
    {
        convention ??=
            new SortConvention(
                x => x.AddDefaults().BindRuntimeType(typeof(TRuntimeType), type.GetType()));

        var builder = SchemaBuilder.New()
            .AddConvention<ISortConvention>(convention)
            .TryAddTypeInterceptor<SortTypeInterceptor>()
            .AddQueryType(
                c =>
                    c.Name("Query")
                        .Field("foo")
                        .Type<StringType>()
                        .Resolve("bar"))
            .AddType(type);

        builder.Create();

        return new ExecutorBuilder(type);
    }

    protected Schema CreateSchema<T>(T type)
        where T : ISortInputType
    {
        var convention = new SortConvention(x => x.AddDefaults());
        var builder = SchemaBuilder.New()
            .AddConvention<ISortConvention>(convention)
            .TryAddTypeInterceptor<SortTypeInterceptor>()
            .AddQueryType(c => c
                .Name("Query")
                .Field("foo")
                .Type<StringType>()
                .Resolve("bar"))
            .AddType(type);

        return builder.Create();
    }
}
