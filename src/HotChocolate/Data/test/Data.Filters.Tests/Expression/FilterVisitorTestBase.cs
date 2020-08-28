using HotChocolate.Types;

namespace HotChocolate.Data.Filters.Expressions
{
    public class FilterVisitorTestBase
    {
        protected ExecutorBuilder CreateProviderTester<T>(
            T type,
            FilterConvention? convention = null)
            where T : IFilterInputType
        {
            convention ??= new FilterConvention(x => x.UseDefault());

            ISchemaBuilder builder = SchemaBuilder.New()
                .AddConvention<IFilterConvention>(convention)
                .AddTypeInterceptor<FilterTypeInterceptor>()
                .AddQueryType(c =>
                    c.Name("Query")
                        .Field("foo")
                        .Type<StringType>()
                        .Resolver("bar"))
                .AddType(type);

            builder.Create();

            return new ExecutorBuilder(type);
        }

        protected ISchema CreateSchema<T>(T type)
            where T : IFilterInputType
        {
            var convention = new FilterConvention(x => x.UseDefault());
            ISchemaBuilder builder = SchemaBuilder.New()
                .AddConvention<IFilterConvention>(convention)
                .AddTypeInterceptor<FilterTypeInterceptor>()
                .AddQueryType(c =>
                    c.Name("Query")
                        .Field("foo")
                        .Type<StringType>()
                        .Resolver("bar"))
                .AddType(type);

            return builder.Create();
        }
    }
}
