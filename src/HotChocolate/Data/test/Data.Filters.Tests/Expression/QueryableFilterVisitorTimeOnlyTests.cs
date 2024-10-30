using HotChocolate.Language;

namespace HotChocolate.Data.Filters.Expressions;

public class QueryableFilterVisitorTimeOnlyTests
    : FilterVisitorTestBase
{
    [Fact]
    public void Create_ShortEqual_Expression()
    {
        // arrange
        var value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
            "{ value: { eq: \"23:59:59\" }}");
        var tester = CreateProviderTester(new FooFilterInput());

        // act
        var func = tester.Build<Foo>(value);

        // assert
        var a = new Foo { Value = new TimeOnly(23, 59, 59), };
        Assert.True(func(a));

        var b = new Foo { Value = new TimeOnly(1, 59, 59), };
        Assert.False(func(b));
    }

    [Fact]
    public void Create_ShortNotEqual_Expression()
    {
        // arrange
        var value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
            "{ value: { neq: \"23:59:59\" }}");
        var tester = CreateProviderTester(new FooFilterInput());

        // act
        var func = tester.Build<Foo>(value);

        // assert
        var a = new Foo { Value = new TimeOnly(1, 59, 59), };
        Assert.True(func(a));

        var b = new Foo { Value = new TimeOnly(23, 59, 59), };
        Assert.False(func(b));
    }

    [Fact]
    public void Create_ShortNullableEqual_Expression()
    {
        // arrange
        var value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
            "{ value: { eq: null }}");
        var tester = CreateProviderTester(new FooNullableFilterInput());

        // act
        var func = tester.Build<FooNullable>(value);

        // assert
        var a = new FooNullable { Value = null, };
        Assert.True(func(a));

        var b = new FooNullable { Value = new TimeOnly(23, 59, 59), };
        Assert.False(func(b));
    }

    public class Foo
    {
        public TimeOnly Value { get; set; }
    }

    public class FooNullable
    {
        public TimeOnly? Value { get; set; }
    }

    public class FooFilterInput
        : FilterInputType<Foo>
    {
    }

    public class FooNullableFilterInput
        : FilterInputType<FooNullable>
    {
    }
}
