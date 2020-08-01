using System;
using System.Linq.Expressions;
using HotChocolate.Data.Filters.Expressions;
using HotChocolate.Language;
using HotChocolate.Types;
using Xunit;

namespace HotChocolate.Data.Filters
{
    public class FilterConventionTests
    {
        [Fact]
        public void FilterConvention_Should_WorkIfConfigurationIsComplete()
        {
            // arrange
            var provider = new QueryableFilterProvider(
                descriptor =>
                {
                    descriptor.AddFieldHandler<QueryableStringEqualsHandler>();
                    descriptor.AddFieldHandler<QueryableDefaultFieldHandler>();
                    descriptor.Visitor<FilterVisitor<Expression, QueryableFilterContext>>();
                    descriptor.Combinator<QueryableCombinator>();
                });

            var convention = new FilterConvention(
                descriptor =>
                {
                    descriptor.Operation(Operations.Equals).Name("eq");
                    descriptor.Binding<string, TestOperationType>();
                    descriptor.Provider(provider);
                });

            IValueNode? value = Utf8GraphQLParser.Syntax.ParseValueLiteral("{ bar: { eq:\"a\" }}");
            var type = new FooFilterType();

            //act
            ISchema? schema = CreateSchemaWith(type, convention);
            var executor = new ExecutorBuilder(type, convention);

            Func<Foo, bool>? func = executor.Build<Foo>(value);

            // assert
            var a = new Foo { Bar = "a" };
            Assert.True(func(a));

            var b = new Foo { Bar = "b" };
            Assert.False(func(b));
        }

        protected ISchema CreateSchemaWith(IFilterInputType type, FilterConvention convention)
        {
            ISchemaBuilder builder = SchemaBuilder.New()
                .AddConvention<IFilterConvention>(convention)
                .UseFiltering()
                .AddQueryType(c =>
                    c.Name("Query")
                        .Field("foo")
                        .Type<StringType>()
                        .Resolver("bar"))
                .AddType(type);

            return builder.Create();
        }

        public class TestOperationType : StringOperationInput
        {
            protected override void Configure(IFilterInputTypeDescriptor descriptor)
            {
                descriptor.Operation(Operations.Equals).Type<StringType>();
                descriptor.UseAnd(false).UseOr(false);
            }
        }

        public class Foo
        {
            public string Bar { get; set; }
        }

        public class FooFilterType
            : FilterInputType<Foo>
        {
            protected override void Configure(
                IFilterInputTypeDescriptor<Foo> descriptor)
            {
                descriptor.Field(t => t.Bar);
                descriptor.UseAnd(false).UseOr(false);
            }
        }
    }
}