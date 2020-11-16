using System;
using HotChocolate.Language;
using Xunit;

namespace HotChocolate.Data.Filters.Expressions
{

    public class QueryableFilterVisitorBooleanTests
        : FilterVisitorTestBase
    {
        [Fact]
        public void Create_BooleanEqual_Expression()
        {
            // arrange
            IValueNode? value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
                "{ bar: { eq: true }}");
            ExecutorBuilder? tester = CreateProviderTester(new FooFilterType());

            // act
            Func<Foo, bool>? func = tester.Build<Foo>(value);

            // assert
            var a = new Foo { Bar = true };
            Assert.True(func(a));

            var b = new Foo { Bar = false };
            Assert.False(func(b));
        }

        [Fact]
        public void Create_BooleanNotEqual_Expression()
        {
            // arrange
            IValueNode? value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
                "{ bar: { eq: false }}");
            ExecutorBuilder? tester = CreateProviderTester(new FooFilterType());

            // act
            Func<Foo, bool>? func = tester.Build<Foo>(value);

            // assert
            var a = new Foo { Bar = false };
            Assert.True(func(a));

            var b = new Foo { Bar = true };
            Assert.False(func(b));
        }

        [Fact]
        public void Create_NullableBooleanEqual_Expression()
        {
            // arrange
            IValueNode? value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
                "{ bar: { eq: true }}");
            ExecutorBuilder? tester = CreateProviderTester(new FooNullableFilterType());

            // act
            Func<FooNullable, bool>? func = tester.Build<FooNullable>(value);

            // assert
            var a = new FooNullable { Bar = true };
            Assert.True(func(a));

            var b = new FooNullable { Bar = false };
            Assert.False(func(b));

            var c = new FooNullable { Bar = null };
            Assert.False(func(c));
        }

        [Fact]
        public void Create_NullableBooleanNotEqual_Expression()
        {
            // arrange
            IValueNode? value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
                "{ bar: { eq: false }}");
            ExecutorBuilder? tester = CreateProviderTester(new FooNullableFilterType());

            // act
            Func<FooNullable, bool>? func = tester.Build<FooNullable>(value);

            // assert
            var a = new FooNullable { Bar = false };
            Assert.True(func(a));

            var b = new FooNullable { Bar = true };
            Assert.False(func(b));

            var c = new FooNullable { Bar = null };
            Assert.False(func(c));
        }

        public class Foo
        {
            public bool Bar { get; set; }
        }

        public class FooNullable
        {
            public bool? Bar { get; set; }
        }

        public class FooFilterType
            : FilterInputType<Foo>
        {
        }

        public class FooNullableFilterType
            : FilterInputType<FooNullable>
        {
        }
    }
}