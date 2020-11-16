using System;
using HotChocolate.Language;
using Xunit;

namespace HotChocolate.Data.Filters.Expressions
{
    public class QueryableFilterVisitorStringTests
        : FilterVisitorTestBase
    {
        [Fact]
        public void Create_StringEqual_Expression()
        {
            // arrange
            IValueNode? value = Utf8GraphQLParser.Syntax.ParseValueLiteral("{ bar: { eq:\"a\" }}");
            ExecutorBuilder? tester = CreateProviderTester(new FooFilterType());

            // act
            Func<Foo, bool>? func = tester.Build<Foo>(value);

            // assert
            var a = new Foo { Bar = "a" };
            Assert.True(func(a));

            var b = new Foo { Bar = "b" };
            Assert.False(func(b));
        }

        [Fact]
        public void Create_StringNotEqual_Expression()
        {
            // arrange
            IValueNode? value = Utf8GraphQLParser.Syntax.ParseValueLiteral("{ bar: { neq:\"a\" }}");
            ExecutorBuilder? tester = CreateProviderTester(new FooFilterType());

            // act
            Func<Foo, bool>? func = tester.Build<Foo>(value);

            // assert
            var a = new Foo { Bar = "a" };
            Assert.False(func(a));

            var b = new Foo { Bar = "b" };
            Assert.True(func(b));
        }

        [Fact]
        public void Create_StringIn_Expression()
        {
            // arrange
            IValueNode? value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
                "{ bar: { in:[\"a\", \"c\"]}}");
            ExecutorBuilder? tester = CreateProviderTester(new FooFilterType());

            // act
            Func<Foo, bool>? func = tester.Build<Foo>(value);

            // assert
            var a = new Foo { Bar = "a" };
            Assert.True(func(a));

            var b = new Foo { Bar = "b" };
            Assert.False(func(b));
        }

        [Fact]
        public void Create_StringIn_SingleValue_Expression()
        {
            // arrange
            IValueNode? value = Utf8GraphQLParser.Syntax.ParseValueLiteral("{ bar: { in:[\"a\"]}}");
            ExecutorBuilder? tester = CreateProviderTester(new FooFilterType());

            // act
            Func<Foo, bool>? func = tester.Build<Foo>(value);

            // assert
            var a = new Foo { Bar = "a" };
            Assert.True(func(a));

            var b = new Foo { Bar = "b" };
            Assert.False(func(b));
        }

        [Fact]
        public void Create_StringNotIn_Expression()
        {
            // arrange
            IValueNode? value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
                "{ bar: { nin:[\"a\", \"c\"]}}");
            ExecutorBuilder? tester = CreateProviderTester(new FooFilterType());

            // act
            Func<Foo, bool>? func = tester.Build<Foo>(value);

            // assert
            var a = new Foo { Bar = "a" };
            Assert.False(func(a));

            var b = new Foo { Bar = "b" };
            Assert.True(func(b));
        }

        [Fact]
        public void Create_StringContains_Expression()
        {
            // arrange
            IValueNode? value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
                "{ bar: { contains:\"a\" }}");
            ExecutorBuilder? tester = CreateProviderTester(new FooFilterType());

            // act
            Func<Foo, bool>? func = tester.Build<Foo>(value);

            // assert
            var a = new Foo { Bar = "testatest" };
            Assert.True(func(a));

            var b = new Foo { Bar = "testbtest" };
            Assert.False(func(b));
        }

        [Fact]
        public void Create_StringNoContains_Expression()
        {
            IValueNode? value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
                "{ bar: { ncontains:\"a\" }}");
            ExecutorBuilder? tester = CreateProviderTester(new FooFilterType());

            // act
            Func<Foo, bool>? func = tester.Build<Foo>(value);

            // assert
            var a = new Foo { Bar = "testatest" };
            Assert.False(func(a));

            var b = new Foo { Bar = "testbtest" };
            Assert.True(func(b));
        }

        [Fact]
        public void Create_StringStartsWith_Expression()
        {
            // arrange
            IValueNode? value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
                "{ bar: { startsWith:\"a\" }}");
            ExecutorBuilder? tester = CreateProviderTester(new FooFilterType());

            // act
            Func<Foo, bool>? func = tester.Build<Foo>(value);

            // assert
            var a = new Foo { Bar = "ab" };
            Assert.True(func(a));

            var b = new Foo { Bar = "ba" };
            Assert.False(func(b));
        }

        [Fact]
        public void Create_StringNotStartsWith_Expression()
        {
            // arrange
            IValueNode? value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
                "{ bar: { nstartsWith:\"a\" }}");
            ExecutorBuilder? tester = CreateProviderTester(new FooFilterType());

            // act
            Func<Foo, bool>? func = tester.Build<Foo>(value);

            // assert
            var a = new Foo { Bar = "ab" };
            Assert.False(func(a));

            var b = new Foo { Bar = "ba" };
            Assert.True(func(b));
        }

        [Fact]
        public void Create_StringEndsWith_Expression()
        {
            // arrange
            IValueNode? value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
                "{ bar: { endsWith:\"a\" }}");
            ExecutorBuilder? tester = CreateProviderTester(new FooFilterType());

            // act
            Func<Foo, bool>? func = tester.Build<Foo>(value);

            // assert
            var a = new Foo { Bar = "ab" };
            Assert.False(func(a));

            var b = new Foo { Bar = "ba" };
            Assert.True(func(b));
        }

        [Fact]
        public void Create_StringNotEndsWith_Expression()
        {
            // arrange
            IValueNode? value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
                "{ bar: { nendsWith:\"a\" }}");
            ExecutorBuilder? tester = CreateProviderTester(new FooFilterType());

            // act
            Func<Foo, bool>? func = tester.Build<Foo>(value);

            // assert
            var a = new Foo { Bar = "ab" };
            Assert.True(func(a));

            var b = new Foo { Bar = "ba" };
            Assert.False(func(b));
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
            }
        }
    }
}