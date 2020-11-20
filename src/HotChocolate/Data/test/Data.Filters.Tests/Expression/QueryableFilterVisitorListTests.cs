using System;
using System.Collections.Generic;
using HotChocolate.Language;
using Xunit;

namespace HotChocolate.Data.Filters.Expressions
{
    public class QueryableFilterVisitorListTests
        : FilterVisitorTestBase
    {

        [Fact]
        public void Create_ArraySomeStringEqual_Expression()
        {
            // arrange
            IValueNode? value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
                "{ bar: {some: { eq: \"a\" }}}");
            ExecutorBuilder? tester = CreateProviderTester(new FooSimpleFilterInput());

            // act
            Func<FooSimple, bool>? func = tester.Build<FooSimple>(value);

            // assert
            var a = new FooSimple { Bar = new[] { "c", "d", "a" } };
            Assert.True(func(a));

            var b = new FooSimple { Bar = new[] { "c", "d", "b" } };
            Assert.False(func(b));
        }

        [Fact]
        public void Create_ArrayAnyStringEqual_Expression()
        {
            // arrange
            IValueNode? value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
                "{ bar: {any: true}}");
            ExecutorBuilder? tester = CreateProviderTester(new FooSimpleFilterInput());

            // act
            Func<FooSimple, bool>? func = tester.Build<FooSimple>(value);

            // assert
            var a = new FooSimple { Bar = new[] { "c", "d", "a" } };
            Assert.True(func(a));

            var b = new FooSimple { Bar = new string[0] };
            Assert.False(func(b));

            var c = new FooSimple { Bar = null };
            Assert.False(func(c));
        }

        [Fact]
        public void Create_ArraySomeStringEqualWithNull_Expression()
        {
            // arrange
            IValueNode? value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
                "{ bar: {some: { eq: \"a\" }}}");
            ExecutorBuilder? tester = CreateProviderTester(new FooSimpleFilterInput());

            // act
            Func<FooSimple, bool>? func = tester.Build<FooSimple>(value);

            // assert
            var a = new FooSimple { Bar = new[] { "c", null, "a" } };
            Assert.True(func(a));

            var b = new FooSimple { Bar = new[] { "c", null, "b" } };
            Assert.False(func(b));

            var c = new FooSimple { Bar = null };
            Assert.False(func(c));
        }

        [Fact]
        public void Create_ArraySomeObjectStringEqualWithNull_Expression()
        {
            // arrange
            IValueNode? value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
                "{ fooNested: {some: {bar: { eq: \"a\" }}}}");
            ExecutorBuilder? tester = CreateProviderTester(new FooFilterInput());

            // act
            Func<Foo, bool>? func = tester.Build<Foo>(value);

            // assert
            var a = new Foo
            {
                FooNested = new[]
                {
                    new FooNested { Bar = "c" },
                    null,
                    new FooNested { Bar = "a" }
                }
            };
            Assert.True(func(a));

            var b = new Foo
            {
                FooNested = new[]
                {
                    new FooNested { Bar = "c" },
                    null,
                    new FooNested { Bar = "b" }
                }
            };
            Assert.False(func(b));
        }
        [Fact]
        public void Create_ArraySomeObjectStringEqual_Expression()
        {
            IValueNode? value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
                "{ fooNested: {some: {bar: { eq: \"a\" }}}}");
            ExecutorBuilder? tester = CreateProviderTester(new FooFilterInput());

            // act
            Func<Foo, bool>? func = tester.Build<Foo>(value);
            // assert
            var a = new Foo
            {
                FooNested = new[]
                {
                    new FooNested { Bar = "c" },
                    new FooNested { Bar = "d" },
                    new FooNested { Bar = "a" }
                }
            };
            Assert.True(func(a));

            var b = new Foo
            {
                FooNested = new[]
                {
                    new FooNested { Bar = "c" },
                    new FooNested { Bar = "d" },
                    new FooNested { Bar = "b" }
                }
            };
            Assert.False(func(b));

            var c = new Foo
            {
                FooNested = new[]
                {
                    null,
                    new FooNested { Bar = null },
                    new FooNested { Bar = "c" },
                    new FooNested { Bar = "d" },
                    new FooNested { Bar = "a" }
                }
            };
            Assert.True(func(c));
        }

        [Fact]
        public void Create_ArrayNoneObjectStringEqual_Expression()
        {
            IValueNode? value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
                "{ fooNested: {none: {bar: { eq: \"a\" }}}}");
            ExecutorBuilder? tester = CreateProviderTester(new FooFilterInput());

            // act
            Func<Foo, bool>? func = tester.Build<Foo>(value);

            // assert
            var a = new Foo
            {
                FooNested = new[]
                {
                    new FooNested { Bar = "c" },
                    new FooNested { Bar = "d" },
                    new FooNested { Bar = "a" }
                }
            };
            Assert.False(func(a));

            var b = new Foo
            {
                FooNested = new[]
                {
                    new FooNested { Bar = "c" },
                    new FooNested { Bar = "d" },
                    new FooNested { Bar = "b" }
                }
            };
            Assert.True(func(b));
            var c = new Foo
            {
                FooNested = new[]
                {
                    null,
                    new FooNested { Bar = "c" },
                    new FooNested { Bar = null },
                    new FooNested { Bar = "b" }
                }
            };
            Assert.True(func(c));
        }

        [Fact]
        public void Create_ArrayAllObjectStringEqual_Expression()
        {
            // arrange
            IValueNode? value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
                "{ fooNested: {all: { bar: {eq: \"a\" }}}}");
            ExecutorBuilder? tester = CreateProviderTester(new FooFilterInput());

            // act
            Func<Foo, bool>? func = tester.Build<Foo>(value);

            // assert
            var a = new Foo
            {
                FooNested = new[]
                {
                    new FooNested { Bar = "a" },
                    new FooNested { Bar = "a" },
                    new FooNested { Bar = "a" }
                }
            };
            Assert.True(func(a));

            var b = new Foo
            {
                FooNested = new[]
                {
                    new FooNested { Bar = "c" },
                    new FooNested { Bar = "a" },
                    new FooNested { Bar = "a" }
                }
            };
            Assert.False(func(b));

            var c = new Foo
            {
                FooNested = new[]
                {
                    new FooNested { Bar = "a" },
                    new FooNested { Bar = "d" },
                    new FooNested { Bar = "b" }
                }
            };
            Assert.False(func(c));

            var d = new Foo
            {
                FooNested = new[]
                {
                    new FooNested { Bar = "c" },
                    new FooNested { Bar = "d" },
                    new FooNested { Bar = "b" }
                }
            };
            Assert.False(func(d));

            var e = new Foo
            {
                FooNested = new[]
                {
                    null,
                    new FooNested { Bar = null },
                    new FooNested { Bar = "d" },
                    new FooNested { Bar = "b" }
                }
            };
            Assert.False(func(e));
        }

        [Fact]
        public void Create_ArrayAnyObjectStringEqual_Expression()
        {
            // arrange
            IValueNode? value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
                "{ fooNested: {any: true}}");
            ExecutorBuilder? tester = CreateProviderTester(new FooFilterInput());

            // act
            Func<Foo, bool>? func = tester.Build<Foo>(value);

            // assert
            var a = new Foo
            {
                FooNested = new[]
                {
                    new FooNested { Bar = "c" },
                    new FooNested { Bar = "d" },
                    new FooNested { Bar = "a" }
                }
            };
            Assert.True(func(a));

            var b = new Foo { FooNested = new FooNested[] { } };
            Assert.False(func(b));
            var c = new Foo { FooNested = null };
            Assert.False(func(c));
            var d = new Foo { FooNested = new FooNested[] { null } };
            Assert.True(func(d));
        }

        [Fact]
        public void Create_ArrayNotAnyObjectStringEqual_Expression()
        {
            // arrange
            IValueNode? value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
                "{ fooNested: {any: false}}");
            ExecutorBuilder? tester = CreateProviderTester(new FooFilterInput());

            // act
            Func<Foo, bool>? func = tester.Build<Foo>(value);

            // assert
            var a = new Foo
            {
                FooNested = new[]
                {
                    new FooNested { Bar = "c" },
                    new FooNested { Bar = "d" },
                    new FooNested { Bar = "a" }
                }
            };
            Assert.False(func(a));

            var b = new Foo { FooNested = new FooNested[] { } };
            Assert.True(func(b));
            var c = new Foo { FooNested = null };
            Assert.False(func(c));

            var d = new Foo { FooNested = new FooNested[] { null } };
            Assert.False(func(d));
        }


        [Fact]
        public void Create_ArraySomeStringEqual_Expression_Null()
        {
            // arrange
            IValueNode? value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
                "{ bar: {some: { eq: null }}}");
            ExecutorBuilder? tester = CreateProviderTester(new FooSimpleFilterInput());

            // act
            Func<FooSimple, bool>? func = tester.Build<FooSimple>(value);

            // assert
            var a = new FooSimple { Bar = new[] { "c", null, "a" } };
            Assert.True(func(a));

            var b = new FooSimple { Bar = new[] { "c", "d", "b" } };
            Assert.False(func(b));
        }

        [Fact]
        public void Create_ArrayNoneStringEqual_Expression_Null()
        {
            // arrange
            IValueNode? value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
                "{ bar: {none: { eq: null }}}");
            ExecutorBuilder? tester = CreateProviderTester(new FooSimpleFilterInput());

            // act
            Func<FooSimple, bool>? func = tester.Build<FooSimple>(value);

            // assert
            var a = new FooSimple { Bar = new[] { "c", "d", "a" } };
            Assert.True(func(a));

            var b = new FooSimple { Bar = new[] { "c", null, "b" } };
            Assert.False(func(b));
        }

        [Fact]
        public void Create_ArrayAllStringEqual_Expression_Null()
        {
            // arrange
            IValueNode? value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
                "{ bar: {all: { eq: null }}}");
            ExecutorBuilder? tester = CreateProviderTester(new FooSimpleFilterInput());

            // act
            Func<FooSimple, bool>? func = tester.Build<FooSimple>(value);

            // assert
            var a = new FooSimple { Bar = new string[] { null, null, null } };
            Assert.True(func(a));

            var b = new FooSimple { Bar = new[] { "c", "d", "b" } };
            Assert.False(func(b));

        }

        public class Foo
        {
            public IEnumerable<FooNested?>? FooNested { get; set; }
        }

        public class FooSimple
        {
            public IEnumerable<string?>? Bar { get; set; }
        }

        public class FooNested
        {
            public string? Bar { get; set; }
        }

        public class FooFilterInput
            : FilterInputType<Foo>
        {
            protected override void Configure(
                IFilterInputTypeDescriptor<Foo> descriptor)
            {
                descriptor.Field(t => t.FooNested);
            }
        }

        public class FooSimpleFilterInput
            : FilterInputType<FooSimple>
        {
            protected override void Configure(
                IFilterInputTypeDescriptor<FooSimple> descriptor)
            {
                descriptor.Field(t => t.Bar);
            }
        }
    }
}
