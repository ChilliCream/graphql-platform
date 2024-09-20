using HotChocolate.Language;

namespace HotChocolate.Data.Filters.Expressions;

public class QueryableFilterVisitorListTests : FilterVisitorTestBase
{
    [Fact]
    public void Create_ArraySomeStringEqual_Expression()
    {
        // arrange
        var value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
            "{ bar: {some: { eq: \"a\" }}}");
        var tester = CreateProviderTester(new FooSimpleFilterInput());

        // act
        var func = tester.Build<FooSimple>(value);

        // assert
        var a = new FooSimple { Bar = new[] { "c", "d", "a", }, };
        Assert.True(func(a));

        var b = new FooSimple { Bar = new[] { "c", "d", "b", }, };
        Assert.False(func(b));
    }

    [Fact]
    public void Create_ArrayAnyStringEqual_Expression()
    {
        // arrange
        var value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
            "{ bar: {any: true}}");
        var tester = CreateProviderTester(new FooSimpleFilterInput());

        // act
        var func = tester.Build<FooSimple>(value);

        // assert
        var a = new FooSimple { Bar = new[] { "c", "d", "a", }, };
        Assert.True(func(a));

        var b = new FooSimple { Bar = new string[0], };
        Assert.False(func(b));

        var c = new FooSimple { Bar = null, };
        Assert.False(func(c));
    }

    [Fact]
    public void Create_ArraySomeStringEqualWithNull_Expression()
    {
        // arrange
        var value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
            "{ bar: {some: { eq: \"a\" }}}");
        var tester = CreateProviderTester(new FooSimpleFilterInput());

        // act
        var func = tester.Build<FooSimple>(value);

        // assert
        var a = new FooSimple { Bar = new[] { "c", null, "a", }, };
        Assert.True(func(a));

        var b = new FooSimple { Bar = new[] { "c", null, "b", }, };
        Assert.False(func(b));

        var c = new FooSimple { Bar = null, };
        Assert.False(func(c));
    }

    [Fact]
    public void Create_ArraySomeObjectStringEqualWithNull_Expression()
    {
        // arrange
        var value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
            "{ fooNested: {some: {bar: { eq: \"a\" }}}}");
        var tester = CreateProviderTester(new FooFilterInput());

        // act
        var func = tester.Build<Foo>(value);

        // assert
        var a = new Foo
        {
            FooNested = new[]
            {
                new FooNested { Bar = "c", },
                null,
                new FooNested { Bar = "a", },
            },
        };
        Assert.True(func(a));

        var b = new Foo
        {
            FooNested = new[]
            {
                new FooNested { Bar = "c", },
                null,
                new FooNested { Bar = "b", },
            },
        };
        Assert.False(func(b));
    }
    [Fact]
    public void Create_ArraySomeObjectStringEqual_Expression()
    {
        var value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
            "{ fooNested: {some: {bar: { eq: \"a\" }}}}");
        var tester = CreateProviderTester(new FooFilterInput());

        // act
        var func = tester.Build<Foo>(value);
        // assert
        var a = new Foo
        {
            FooNested = new[]
            {
                new FooNested { Bar = "c", },
                new FooNested { Bar = "d", },
                new FooNested { Bar = "a", },
            },
        };
        Assert.True(func(a));

        var b = new Foo
        {
            FooNested = new[]
            {
                new FooNested { Bar = "c", },
                new FooNested { Bar = "d", },
                new FooNested { Bar = "b", },
            },
        };
        Assert.False(func(b));

        var c = new Foo
        {
            FooNested = new[]
            {
                null,
                new FooNested { Bar = null, },
                new FooNested { Bar = "c", },
                new FooNested { Bar = "d", },
                new FooNested { Bar = "a", },
            },
        };
        Assert.True(func(c));
    }

    [Fact]
    public void Create_ArrayNoneObjectStringEqual_Expression()
    {
        var value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
            "{ fooNested: {none: {bar: { eq: \"a\" }}}}");
        var tester = CreateProviderTester(new FooFilterInput());

        // act
        var func = tester.Build<Foo>(value);

        // assert
        var a = new Foo
        {
            FooNested = new[]
            {
                new FooNested { Bar = "c", },
                new FooNested { Bar = "d", },
                new FooNested { Bar = "a", },
            },
        };
        Assert.False(func(a));

        var b = new Foo
        {
            FooNested = new[]
            {
                new FooNested { Bar = "c", },
                new FooNested { Bar = "d", },
                new FooNested { Bar = "b", },
            },
        };
        Assert.True(func(b));
        var c = new Foo
        {
            FooNested = new[]
            {
                null,
                new FooNested { Bar = "c", },
                new FooNested { Bar = null, },
                new FooNested { Bar = "b", },
            },
        };
        Assert.True(func(c));
    }

    [Fact]
    public void Create_ArrayAllObjectStringEqual_Expression()
    {
        // arrange
        var value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
            "{ fooNested: {all: { bar: {eq: \"a\" }}}}");
        var tester = CreateProviderTester(new FooFilterInput());

        // act
        var func = tester.Build<Foo>(value);

        // assert
        var a = new Foo
        {
            FooNested = new[]
            {
                new FooNested { Bar = "a", },
                new FooNested { Bar = "a", },
                new FooNested { Bar = "a", },
            },
        };
        Assert.True(func(a));

        var b = new Foo
        {
            FooNested = new[]
            {
                new FooNested { Bar = "c", },
                new FooNested { Bar = "a", },
                new FooNested { Bar = "a", },
            },
        };
        Assert.False(func(b));

        var c = new Foo
        {
            FooNested = new[]
            {
                new FooNested { Bar = "a", },
                new FooNested { Bar = "d", },
                new FooNested { Bar = "b", },
            },
        };
        Assert.False(func(c));

        var d = new Foo
        {
            FooNested = new[]
            {
                new FooNested { Bar = "c", },
                new FooNested { Bar = "d", },
                new FooNested { Bar = "b", },
            },
        };
        Assert.False(func(d));

        var e = new Foo
        {
            FooNested = new[]
            {
                null,
                new FooNested { Bar = null, },
                new FooNested { Bar = "d", },
                new FooNested { Bar = "b", },
            },
        };
        Assert.False(func(e));
    }

    [Fact]
    public void Create_ArrayAnyObjectStringEqual_Expression()
    {
        // arrange
        var value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
            "{ fooNested: {any: true}}");
        var tester = CreateProviderTester(new FooFilterInput());

        // act
        var func = tester.Build<Foo>(value);

        // assert
        var a = new Foo
        {
            FooNested = new[]
            {
                new FooNested { Bar = "c", },
                new FooNested { Bar = "d", },
                new FooNested { Bar = "a", },
            },
        };
        Assert.True(func(a));

        var b = new Foo { FooNested = new FooNested[] { }, };
        Assert.False(func(b));
        var c = new Foo { FooNested = null, };
        Assert.False(func(c));
        var d = new Foo { FooNested = new FooNested[] { null!, }, };
        Assert.True(func(d));
    }

    [Fact]
    public void Create_ArrayNotAnyObjectStringEqual_Expression()
    {
        // arrange
        var value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
            "{ fooNested: {any: false}}");
        var tester = CreateProviderTester(new FooFilterInput());

        // act
        var func = tester.Build<Foo>(value);

        // assert
        var a = new Foo
        {
            FooNested = new[]
            {
                new FooNested { Bar = "c", },
                new FooNested { Bar = "d", },
                new FooNested { Bar = "a", },
            },
        };
        Assert.False(func(a));

        var b = new Foo { FooNested = new FooNested[] { }, };
        Assert.True(func(b));
        var c = new Foo { FooNested = null, };
        Assert.False(func(c));

        var d = new Foo { FooNested = new FooNested[] { null!, }, };
        Assert.False(func(d));
    }

    [Fact]
    public void Create_ArraySomeStringEqual_Expression_Null()
    {
        // arrange
        var value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
            "{ bar: {some: { eq: null }}}");
        var tester = CreateProviderTester(new FooSimpleFilterInput());

        // act
        var func = tester.Build<FooSimple>(value);

        // assert
        var a = new FooSimple { Bar = new[] { "c", null, "a", }, };
        Assert.True(func(a));

        var b = new FooSimple { Bar = new[] { "c", "d", "b", }, };
        Assert.False(func(b));
    }

    [Fact]
    public void Create_ArrayNoneStringEqual_Expression_Null()
    {
        // arrange
        var value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
            "{ bar: {none: { eq: null }}}");
        var tester = CreateProviderTester(new FooSimpleFilterInput());

        // act
        var func = tester.Build<FooSimple>(value);

        // assert
        var a = new FooSimple { Bar = new[] { "c", "d", "a", }, };
        Assert.True(func(a));

        var b = new FooSimple { Bar = new[] { "c", null, "b", }, };
        Assert.False(func(b));
    }

    [Fact]
    public void Create_ArrayAllStringEqual_Expression_Null()
    {
        // arrange
        var value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
            "{ bar: {all: { eq: null }}}");
        var tester = CreateProviderTester(new FooSimpleFilterInput());

        // act
        var func = tester.Build<FooSimple>(value);

        // assert
        var a = new FooSimple { Bar = new string[] { null!, null!, null!, }, };
        Assert.True(func(a));

        var b = new FooSimple { Bar = new[] { "c", "d", "b", }, };
        Assert.False(func(b));
    }

    [Fact]
    public void Create_ArraySomeStringEqual_Multiple()
    {
        // arrange
        var value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
            "{ bar: {some: { eq: \"a\" }} otherProperty: {eq:null}}");
        var tester = CreateProviderTester(new FooSimpleFilterInput());

        // act
        var func = tester.Build<FooSimple>(value);

        // assert
        var a = new FooSimple { Bar = new[] { "c", "d", "a", }, };
        Assert.True(func(a));

        var b = new FooSimple { Bar = new[] { "c", "d", "b", }, };
        Assert.False(func(b));
    }

    [Fact]
    public void Create_ArraySomeObjectEqual_Multiple()
    {
        // arrange
        var value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
            "{ fooNested: {some: {bar: { eq: \"a\" }}} otherProperty: {eq:null}}");
        var tester = CreateProviderTester(new FooFilterInput());

        // act
        var func = tester.Build<Foo>(value);

        // assert
        var a = new Foo
        {
            FooNested = new[]
            {
                new FooNested { Bar = "a", },
                new FooNested { Bar = "a", },
                new FooNested { Bar = "a", },
            },
        };
        Assert.True(func(a));

        var b = new Foo
        {
            FooNested = new[]
            {
                new FooNested { Bar = "c", },
                new FooNested { Bar = "a", },
                new FooNested { Bar = "a", },
            },
        };
        Assert.True(func(b));

        var c = new Foo
        {
            FooNested = new[]
            {
                new FooNested { Bar = "a", },
                new FooNested { Bar = "d", },
                new FooNested { Bar = "b", },
            },
        };
        Assert.True(func(c));

        var d = new Foo
        {
            FooNested = new[]
            {
                new FooNested { Bar = "c", },
                new FooNested { Bar = "d", },
                new FooNested { Bar = "b", },
            },
        };
        Assert.False(func(d));

        var e = new Foo
        {
            FooNested = new[]
            {
                null,
                new FooNested { Bar = null, },
                new FooNested { Bar = "d", },
                new FooNested { Bar = "b", },
            },
        };
        Assert.False(func(e));

        var f = new Foo
        {
            OtherProperty = "ShouldBeNull",
            FooNested = new[]
            {
                new FooNested { Bar = "c", },
                new FooNested { Bar = "a", },
                new FooNested { Bar = "a", },
            },
        };
        Assert.False(func(f));
    }

    public class Foo
    {
        public IEnumerable<FooNested?>? FooNested { get; set; }

        public string? OtherProperty { get; set; }
    }

    public class FooSimple
    {
        public IEnumerable<string?>? Bar { get; set; }

        public string? OtherProperty { get; set; }
    }

    public class FooNested
    {
        public string? Bar { get; set; }
    }

    public class FooFilterInput : FilterInputType<Foo>
    {
        protected override void Configure(
            IFilterInputTypeDescriptor<Foo> descriptor)
        {
            descriptor.Field(t => t.FooNested);
        }
    }

    public class FooSimpleFilterInput : FilterInputType<FooSimple>
    {
        protected override void Configure(
            IFilterInputTypeDescriptor<FooSimple> descriptor)
        {
            descriptor.Field(t => t.Bar);
        }
    }
}
