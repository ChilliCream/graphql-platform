using System;
using System.Linq.Expressions;
using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Data.Filters.Expressions
{
    public class FilterVisitorTestBase
    {
        protected ExecutorBuilder CreateProviderTester<T>(T type)
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

            builder.Create();

            return new ExecutorBuilder(type, convention);
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
