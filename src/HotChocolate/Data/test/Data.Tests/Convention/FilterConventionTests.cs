using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using System.Text.Json;
using HotChocolate.Data.Filters.Expressions;
using HotChocolate.Language;
using HotChocolate.Types;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Data.Filters
{
    public class FilterConventionTests
    {
        [Fact]
        public void FilterConvention_Should_Work_When_ConfigurationIsComplete()
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

        [Fact]
        public void FilterConvention_Should_Fail_When_OperationHandlerIsNotRegistered()
        {
            // arrange
            var provider = new QueryableFilterProvider(
                descriptor =>
                {
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

            var type = new FooFilterType();

            //act
            SchemaException? error =
                Assert.Throws<SchemaException>(() => CreateSchemaWith(type, convention));

            Assert.Single(error.Errors);
            error.Errors[0].Message.MatchSnapshot();
        }

        [Fact]
        public void FilterConvention_Should_Fail_When_FieldHandlerIsNotRegistered()
        {
            // arrange
            var provider = new QueryableFilterProvider(
                descriptor =>
                {
                    descriptor.AddFieldHandler<QueryableStringEqualsHandler>();
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

            var type = new FooFilterType();

            //act
            SchemaException? error =
                Assert.Throws<SchemaException>(() => CreateSchemaWith(type, convention));

            Assert.Single(error.Errors);
            error.Errors[0].Message.MatchSnapshot();
        }

        [Fact]
        public void FilterConvention_Should_Fail_When_VisitorIsNotRegistered()
        {
            // arrange
            var provider = new QueryableFilterProvider(
                descriptor =>
                {
                    descriptor.AddFieldHandler<QueryableStringEqualsHandler>();
                    descriptor.AddFieldHandler<QueryableDefaultFieldHandler>();
                    descriptor.Combinator<QueryableCombinator>();
                });

            var convention = new FilterConvention(
                descriptor =>
                {
                    descriptor.Operation(Operations.Equals).Name("eq");
                    descriptor.Binding<string, TestOperationType>();
                    descriptor.Provider(provider);
                });

            var type = new FooFilterType();

            //act
            SchemaException? error =
                Assert.Throws<SchemaException>(() => CreateSchemaWith(type, convention));

            Assert.Single(error.Errors);
            error.Errors[0].Message.MatchSnapshot();
        }

        [Fact]
        public void FilterConvention_Should_Fail_When_CombinatorIsNotRegistered()
        {
            // arrange
            var provider = new QueryableFilterProvider(
                descriptor =>
                {
                    descriptor.AddFieldHandler<QueryableStringEqualsHandler>();
                    descriptor.AddFieldHandler<QueryableDefaultFieldHandler>();
                    descriptor.Visitor<FilterVisitor<Expression, QueryableFilterContext>>();
                });

            var convention = new FilterConvention(
                descriptor =>
                {
                    descriptor.Operation(Operations.Equals).Name("eq");
                    descriptor.Binding<string, TestOperationType>();
                    descriptor.Provider(provider);
                });

            var type = new FooFilterType();

            //act
            SchemaException? error =
                Assert.Throws<SchemaException>(() => CreateSchemaWith(type, convention));

            Assert.Single(error.Errors);
            error.Errors[0].Message.MatchSnapshot();
        }

        [Fact]
        public void FilterConvention_Should_Fail_When_OperationsInUknown()
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
                    descriptor.Binding<string, TestOperationType>();
                    descriptor.Provider(provider);
                });

            var type = new FooFilterType();

            //act
            SchemaException? error =
                Assert.Throws<SchemaException>(() => CreateSchemaWith(type, convention));

            Assert.Single(error.Errors);
            error.Errors[0].Message.MatchSnapshot();
        }

        [Fact]
        public void FilterConvention_Should_Fail_When_OperationsIsNotNamed()
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
                    descriptor.Operation(Operations.Equals).Description("eq");
                    descriptor.Binding<string, TestOperationType>();
                    descriptor.Provider(provider);
                });

            var type = new FooFilterType();

            //act
            ArgumentException? error =
                Assert.Throws<ArgumentException>(() => CreateSchemaWith(type, convention));

            error.Message.MatchSnapshot();
        }

        [Fact]
        public void FilterConvention_Should_Fail_When_NoProviderWasRegistered()
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
                });

            var type = new FooFilterType();

            //act
            SchemaException? error =
                Assert.Throws<SchemaException>(() => CreateSchemaWith(type, convention));

            Assert.Single(error.Errors);
            error.Errors[0].Message.MatchSnapshot();
        }

        [Fact]
        public void FilterConvention_Should_Fail_When_CombinatorDoesNotMatchProvider()
        {
            // arrange
            var provider = new QueryableFilterProvider(
                descriptor =>
                {
                    descriptor.AddFieldHandler<QueryableStringEqualsHandler>();
                    descriptor.AddFieldHandler<QueryableDefaultFieldHandler>();
                    descriptor.Visitor<FilterVisitor<Expression, QueryableFilterContext>>();
                    descriptor.Combinator<FailingCombinator>();
                });

            var convention = new FilterConvention(
                descriptor =>
                {
                    descriptor.Operation(Operations.Equals).Name("eq");
                    descriptor.Binding<string, TestOperationType>();
                    descriptor.Provider(provider);
                });

            var type = new FooFilterType();

            //act
            SchemaException? error =
                Assert.Throws<SchemaException>(() => CreateSchemaWith(type, convention));

            Assert.Single(error.Errors);
            error.Errors[0].Message.MatchSnapshot();
        }

        [Fact]
        public void FilterConvention_Should_Fail_When_NoMatchingBindingWasFound()
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
                    descriptor.Provider(provider);
                });

            var type = new FooFilterType();

            //act
            SchemaException? error =
                Assert.Throws<SchemaException>(() => CreateSchemaWith(type, convention));

            Assert.Single(error.Errors);
            error.Errors[0].Message.MatchSnapshot();
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

        public class FailingCombinator
            : FilterOperationCombinator<string
            , FilterVisitorContext<string>>
        {
            public override bool TryCombineOperations(
                FilterVisitorContext<string> context,
                Queue<string> operations,
                FilterCombinator combinator,
                [NotNullWhen(true)] out string combined)
            {
                throw new NotImplementedException();
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