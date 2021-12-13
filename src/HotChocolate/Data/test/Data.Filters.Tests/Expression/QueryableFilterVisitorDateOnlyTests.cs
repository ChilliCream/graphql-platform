using System;
using HotChocolate.Language;
using HotChocolate.Types;
using Snapshooter.Xunit;
using Xunit;

namespace HotChocolate.Data.Filters.Expressions;

public class QueryableFilterVisitorDateOnlyTests
    : FilterVisitorTestBase
{
#if NET6_0_OR_GREATER
    [Fact]
    public void Create_ShortEqual_Expression()
    {
        // arrange
        IValueNode value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
            "{ value: { eq: \"2020-12-12\" }}");
        ExecutorBuilder tester = CreateProviderTester(new FooFilterInput());

        // act
        Func<Foo, bool> func = tester.Build<Foo>(value);

        // assert
        var a = new Foo { Value = new DateOnly(2020,12,12) };
        Assert.True(func(a));

        var b = new Foo { Value = new DateOnly(2020,12,13) };
        Assert.False(func(b));
    }

    [Fact]
    public void Create_ShortNotEqual_Expression()
    {
        // arrange
        IValueNode value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
            "{ value: { neq: \"2020-12-12\" }}");
        ExecutorBuilder tester = CreateProviderTester(new FooFilterInput());

        // act
        Func<Foo, bool> func = tester.Build<Foo>(value);


        // assert
        var a = new Foo { Value = new DateOnly(2020,12,13) };
        Assert.True(func(a));

        var b = new Foo { Value = new DateOnly(2020,12,12)};
        Assert.False(func(b));
    }

    [Fact]
    public void Create_ShortNullableEqual_Expression()
    {
        // arrange
        IValueNode value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
            "{ value: { eq: null }}");
        ExecutorBuilder tester = CreateProviderTester(new FooNullableFilterInput());

        // act
        Func<FooNullable, bool> func = tester.Build<FooNullable>(value);

        // assert
        var a = new FooNullable { Value = null };
        Assert.True(func(a));

        var b = new FooNullable { Value = new DateOnly(2020,12,13) };
        Assert.False(func(b));
    }

    public class Foo
    {
        public DateOnly Value { get; set; }
    }

    public class FooNullable
    {
        public DateOnly? Value { get; set; }
    }

    public class FooFilterInput
        : FilterInputType<Foo>
    {
    }
    public class FooNullableFilterInput
        : FilterInputType<FooNullable>
    {
    }
#endif
}
