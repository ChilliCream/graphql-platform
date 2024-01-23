using HotChocolate.Language;

namespace HotChocolate.Data.Filters.Expressions;

public class QueryableFilterVisitorStringTests
    : FilterVisitorTestBase
{
    [Fact]
    public void Create_StringEqual_Expression()
    {
        // arrange
        var value = Utf8GraphQLParser.Syntax.ParseValueLiteral("{ bar: { eq:\"a\" }}");
        var tester = CreateProviderTester(new FooFilterInput());

        // act
        var func = tester.Build<Foo>(value);

        // assert
        var a = new Foo { Bar = "a", };
        Assert.True(func(a));

        var b = new Foo { Bar = "b", };
        Assert.False(func(b));
    }

    [Fact]
    public void Create_StringNotEqual_Expression()
    {
        // arrange
        var value = Utf8GraphQLParser.Syntax.ParseValueLiteral("{ bar: { neq:\"a\" }}");
        var tester = CreateProviderTester(new FooFilterInput());

        // act
        var func = tester.Build<Foo>(value);

        // assert
        var a = new Foo { Bar = "a", };
        Assert.False(func(a));

        var b = new Foo { Bar = "b", };
        Assert.True(func(b));
    }

    [Fact]
    public void Create_StringIn_Expression()
    {
        // arrange
        var value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
            "{ bar: { in:[\"a\", \"c\"]}}");
        var tester = CreateProviderTester(new FooFilterInput());

        // act
        var func = tester.Build<Foo>(value);

        // assert
        var a = new Foo { Bar = "a", };
        Assert.True(func(a));

        var b = new Foo { Bar = "b", };
        Assert.False(func(b));
    }

    [Fact]
    public void Create_StringIn_SingleValue_Expression()
    {
        // arrange
        var value = Utf8GraphQLParser.Syntax.ParseValueLiteral("{ bar: { in:[\"a\"]}}");
        var tester = CreateProviderTester(new FooFilterInput());

        // act
        var func = tester.Build<Foo>(value);

        // assert
        var a = new Foo { Bar = "a", };
        Assert.True(func(a));

        var b = new Foo { Bar = "b", };
        Assert.False(func(b));
    }

    [Fact]
    public void Create_StringNotIn_Expression()
    {
        // arrange
        var value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
            "{ bar: { nin:[\"a\", \"c\"]}}");
        var tester = CreateProviderTester(new FooFilterInput());

        // act
        var func = tester.Build<Foo>(value);

        // assert
        var a = new Foo { Bar = "a", };
        Assert.False(func(a));

        var b = new Foo { Bar = "b", };
        Assert.True(func(b));
    }

    [Fact]
    public void Create_StringContains_Expression()
    {
        // arrange
        var value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
            "{ bar: { contains:\"a\" }}");
        var tester = CreateProviderTester(new FooFilterInput());

        // act
        var func = tester.Build<Foo>(value);

        // assert
        var a = new Foo { Bar = "testatest", };
        Assert.True(func(a));

        var b = new Foo { Bar = "testbtest", };
        Assert.False(func(b));
    }

    [Fact]
    public void Create_StringNoContains_Expression()
    {
        var value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
            "{ bar: { ncontains:\"a\" }}");
        var tester = CreateProviderTester(new FooFilterInput());

        // act
        var func = tester.Build<Foo>(value);

        // assert
        var a = new Foo { Bar = "testatest", };
        Assert.False(func(a));

        var b = new Foo { Bar = "testbtest", };
        Assert.True(func(b));
    }

    [Fact]
    public void Create_StringStartsWith_Expression()
    {
        // arrange
        var value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
            "{ bar: { startsWith:\"a\" }}");
        var tester = CreateProviderTester(new FooFilterInput());

        // act
        var func = tester.Build<Foo>(value);

        // assert
        var a = new Foo { Bar = "ab", };
        Assert.True(func(a));

        var b = new Foo { Bar = "ba", };
        Assert.False(func(b));
    }

    [Fact]
    public void Create_StringNotStartsWith_Expression()
    {
        // arrange
        var value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
            "{ bar: { nstartsWith:\"a\" }}");
        var tester = CreateProviderTester(new FooFilterInput());

        // act
        var func = tester.Build<Foo>(value);

        // assert
        var a = new Foo { Bar = "ab", };
        Assert.False(func(a));

        var b = new Foo { Bar = "ba", };
        Assert.True(func(b));
    }

    [Fact]
    public void Create_StringEndsWith_Expression()
    {
        // arrange
        var value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
            "{ bar: { endsWith:\"a\" }}");
        var tester = CreateProviderTester(new FooFilterInput());

        // act
        var func = tester.Build<Foo>(value);

        // assert
        var a = new Foo { Bar = "ab", };
        Assert.False(func(a));

        var b = new Foo { Bar = "ba", };
        Assert.True(func(b));
    }

    [Fact]
    public void Create_StringNotEndsWith_Expression()
    {
        // arrange
        var value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
            "{ bar: { nendsWith:\"a\" }}");
        var tester = CreateProviderTester(new FooFilterInput());

        // act
        var func = tester.Build<Foo>(value);

        // assert
        var a = new Foo { Bar = "ab", };
        Assert.True(func(a));

        var b = new Foo { Bar = "ba", };
        Assert.False(func(b));
    }

    public class Foo
    {
        public string? Bar { get; set; }
    }

    public class FooFilterInput
        : FilterInputType<Foo>
    {
        protected override void Configure(
            IFilterInputTypeDescriptor<Foo> descriptor)
        {
            descriptor.Field(t => t.Bar);
        }
    }
}
