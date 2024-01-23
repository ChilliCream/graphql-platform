using HotChocolate.Language;

namespace HotChocolate.Data.Filters.Expressions;

public class QueryableFilterVisitorBooleanTests : FilterVisitorTestBase
{
    [Fact]
    public void Create_BooleanEqual_Expression()
    {
        // arrange
        var value = Utf8GraphQLParser.Syntax.ParseValueLiteral("{ bar: { eq: true }}");
        var tester = CreateProviderTester(new FooFilterInput());

        // act
        var func = tester.Build<Foo>(value);

        // assert
        var a = new Foo { Bar = true, };
        Assert.True(func(a));

        var b = new Foo { Bar = false, };
        Assert.False(func(b));
    }

    [Fact]
    public void Create_BooleanNotEqual_Expression()
    {
        // arrange
        var value = Utf8GraphQLParser.Syntax.ParseValueLiteral("{ bar: { eq: false }}");
        var tester = CreateProviderTester(new FooFilterInput());

        // act
        var func = tester.Build<Foo>(value);

        // assert
        var a = new Foo { Bar = false, };
        Assert.True(func(a));

        var b = new Foo { Bar = true, };
        Assert.False(func(b));
    }

    [Fact]
    public void Create_NullableBooleanEqual_Expression()
    {
        // arrange
        var value = Utf8GraphQLParser.Syntax.ParseValueLiteral("{ bar: { eq: true }}");
        var tester = CreateProviderTester(new FooNullableFilterInput());

        // act
        var func = tester.Build<FooNullable>(value);

        // assert
        var a = new FooNullable { Bar = true, };
        Assert.True(func(a));

        var b = new FooNullable { Bar = false, };
        Assert.False(func(b));

        var c = new FooNullable { Bar = null, };
        Assert.False(func(c));
    }

    [Fact]
    public void Create_NullableBooleanNotEqual_Expression()
    {
        // arrange
        var value = Utf8GraphQLParser.Syntax.ParseValueLiteral("{ bar: { eq: false }}");
        var tester = CreateProviderTester(new FooNullableFilterInput());

        // act
        var func = tester.Build<FooNullable>(value);

        // assert
        var a = new FooNullable { Bar = false, };
        Assert.True(func(a));

        var b = new FooNullable { Bar = true, };
        Assert.False(func(b));

        var c = new FooNullable { Bar = null, };
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

    public class FooFilterInput : FilterInputType<Foo>
    {
    }

    public class FooNullableFilterInput : FilterInputType<FooNullable>
    {
    }
}
