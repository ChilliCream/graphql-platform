using HotChocolate.Language;
using HotChocolate.Types;

namespace HotChocolate.Data.Filters.Expressions;

public class QueryableFilterVisitorComparableTests
    : FilterVisitorTestBase
{
    [Fact]
    public void Create_ShortEqual_Expression()
    {
        // arrange
        var value = Utf8GraphQLParser.Syntax.ParseValueLiteral("{ barShort: { eq: 12 }}");
        var tester = CreateProviderTester(new FooFilterInput());

        // act
        var func = tester.Build<Foo>(value);

        // assert
        var a = new Foo { BarShort = 12, };
        Assert.True(func(a));

        var b = new Foo { BarShort = 13, };
        Assert.False(func(b));
    }

    [Fact]
    public void Create_ShortNotEqual_Expression()
    {
        // arrange
        var value = Utf8GraphQLParser.Syntax.ParseValueLiteral("{ barShort: { neq: 12 }}");
        var tester = CreateProviderTester(new FooFilterInput());

        // act
        var func = tester.Build<Foo>(value);

        // assert
        var a = new Foo { BarShort = 13, };
        Assert.True(func(a));

        var b = new Foo { BarShort = 12, };
        Assert.False(func(b));
    }

    [Fact]
    public void Create_ShortGreaterThan_Expression()
    {
        // arrange
        var value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
            "{ barShort: { gt: 12 }}");
        var tester = CreateProviderTester(new FooFilterInput());

        // act
        var func = tester.Build<Foo>(value);

        // assert
        var a = new Foo { BarShort = 11, };
        Assert.False(func(a));

        var b = new Foo { BarShort = 12, };
        Assert.False(func(b));

        var c = new Foo { BarShort = 13, };
        Assert.True(func(c));
    }

    [Fact]
    public void Create_ShortNotGreaterThan_Expression()
    {
        // arrange
        var value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
            "{ barShort: { ngt: 12 }}");
        var tester = CreateProviderTester(new FooFilterInput());

        // act
        var func = tester.Build<Foo>(value);

        // assert
        var a = new Foo { BarShort = 11, };
        Assert.True(func(a));

        var b = new Foo { BarShort = 12, };
        Assert.True(func(b));

        var c = new Foo { BarShort = 13, };
        Assert.False(func(c));
    }

    [Fact]
    public void Create_ShortGreaterThanOrEquals_Expression()
    {
        // arrange
        var value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
            "{ barShort: { gte: 12 }}");
        var tester = CreateProviderTester(new FooFilterInput());

        // act
        var func = tester.Build<Foo>(value);

        // assert
        var a = new Foo { BarShort = 11, };
        Assert.False(func(a));

        var b = new Foo { BarShort = 12, };
        Assert.True(func(b));

        var c = new Foo { BarShort = 13, };
        Assert.True(func(c));
    }

    [Fact]
    public void Create_ShortNotGreaterThanOrEquals_Expression()
    {
        // arrange
        var value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
            "{ barShort: { ngte: 12 }}");
        var tester = CreateProviderTester(new FooFilterInput());

        // act
        var func = tester.Build<Foo>(value);

        // assert
        var a = new Foo { BarShort = 11, };
        Assert.True(func(a));

        var b = new Foo { BarShort = 12, };
        Assert.False(func(b));

        var c = new Foo { BarShort = 13, };
        Assert.False(func(c));
    }

    [Fact]
    public void Create_ShortLowerThan_Expression()
    {
        // arrange
        var value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
            "{ barShort: { lt: 12 }}");
        var tester = CreateProviderTester(new FooFilterInput());

        // act
        var func = tester.Build<Foo>(value);

        // assert
        var a = new Foo { BarShort = 11, };
        Assert.True(func(a));

        var b = new Foo { BarShort = 12, };
        Assert.False(func(b));

        var c = new Foo { BarShort = 13, };
        Assert.False(func(c));
    }

    [Fact]
    public void Create_ShortNotLowerThan_Expression()
    {
        // arrange
        var value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
            "{ barShort: { nlt: 12 }}");
        var tester = CreateProviderTester(new FooFilterInput());

        // act
        var func = tester.Build<Foo>(value);

        // assert
        var a = new Foo { BarShort = 11, };
        Assert.False(func(a));

        var b = new Foo { BarShort = 12, };
        Assert.True(func(b));

        var c = new Foo { BarShort = 13, };
        Assert.True(func(c));
    }

    [Fact]
    public void Create_ShortLowerThanOrEquals_Expression()
    {
        // arrange
        var value = Utf8GraphQLParser.Syntax.ParseValueLiteral("{ barShort: { lte: 12 }}");
        var tester = CreateProviderTester(new FooFilterInput());

        // act
        var func = tester.Build<Foo>(value);

        // assert
        var a = new Foo { BarShort = 11, };
        Assert.True(func(a));

        var b = new Foo { BarShort = 12, };
        Assert.True(func(b));

        var c = new Foo { BarShort = 13, };
        Assert.False(func(c));
    }

    [Fact]
    public void Create_ShortNotLowerThanOrEquals_Expression()
    {
        // arrange
        var value = Utf8GraphQLParser.Syntax.ParseValueLiteral("{ barShort: { nlte: 12 }}");
        var tester = CreateProviderTester(new FooFilterInput());

        // act
        var func = tester.Build<Foo>(value);

        // assert
        var a = new Foo { BarShort = 11, };
        Assert.False(func(a));

        var b = new Foo { BarShort = 12, };
        Assert.False(func(b));

        var c = new Foo { BarShort = 13, };
        Assert.True(func(c));
    }

    [Fact]
    public void Create_ShortIn_Expression()
    {
        // arrange
        var value = Utf8GraphQLParser.Syntax.ParseValueLiteral("{ barShort: { in: [13, 14] }}");
        var tester = CreateProviderTester(new FooFilterInput());

        // act
        var func = tester.Build<Foo>(value);

        // assert
        var a = new Foo { BarShort = 13, };
        Assert.True(func(a));

        var b = new Foo { BarShort = 12, };
        Assert.False(func(b));
    }

    [Fact]
    public void Create_ShortNotIn_Expression()
    {
        // arrange
        var value = Utf8GraphQLParser.Syntax.ParseValueLiteral("{ barShort: { nin: [13, 14] }}");
        var tester = CreateProviderTester(new FooFilterInput());

        // act
        var func = tester.Build<Foo>(value);

        // assert
        var a = new Foo { BarShort = 12, };
        Assert.True(func(a));

        var b = new Foo { BarShort = 13, };
        Assert.False(func(b));
    }

    [Fact]
    public void Create_NullableShortEqual_Expression()
    {
        // arrange
        var value = Utf8GraphQLParser.Syntax.ParseValueLiteral("{ barShort: { eq: 12 }}");
        var tester = CreateProviderTester(new FooNullableFilterInput());

        // act
        var func = tester.Build<FooNullable>(value);

        // assert
        var a = new FooNullable { BarShort = 12, };
        Assert.True(func(a));

        var b = new FooNullable { BarShort = 13, };
        Assert.False(func(b));

        var c = new FooNullable { BarShort = null, };
        Assert.False(func(c));
    }

    [Fact]
    public void Create_NullableShortNotEqual_Expression()
    {
        // arrange
        var value = Utf8GraphQLParser.Syntax.ParseValueLiteral("{ barShort: { neq: 12 }}");
        var tester = CreateProviderTester(new FooNullableFilterInput());

        // act
        var func = tester.Build<FooNullable>(value);

        // assert
        var a = new FooNullable { BarShort = 13, };
        Assert.True(func(a));

        var b = new FooNullable { BarShort = 12, };
        Assert.False(func(b));

        var c = new FooNullable { BarShort = null, };
        Assert.True(func(c));
    }

    [Fact]
    public void Create_NullableShortGreaterThan_Expression()
    {
        // arrange
        var value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
            "{ barShort: { gt: 12 }}");
        var tester = CreateProviderTester(new FooNullableFilterInput());

        // act
        var func = tester.Build<FooNullable>(value);

        // assert
        var a = new FooNullable { BarShort = 11, };
        Assert.False(func(a));

        var b = new FooNullable { BarShort = 12, };
        Assert.False(func(b));

        var c = new FooNullable { BarShort = 13, };
        Assert.True(func(c));

        var d = new FooNullable { BarShort = null, };
        Assert.False(func(d));
    }

    [Fact]
    public void Create_NullableShortNotGreaterThan_Expression()
    {
        // arrange
        var value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
            "{ barShort: { ngt: 12 }}");
        var tester = CreateProviderTester(new FooNullableFilterInput());

        // act
        var func = tester.Build<FooNullable>(value);

        // assert
        var a = new FooNullable { BarShort = 11, };
        Assert.True(func(a));

        var b = new FooNullable { BarShort = 12, };
        Assert.True(func(b));

        var c = new FooNullable { BarShort = 13, };
        Assert.False(func(c));

        var d = new FooNullable { BarShort = null, };
        Assert.True(func(d));
    }

    [Fact]
    public void Create_NullableShortGreaterThanOrEquals_Expression()
    {
        // arrange
        var value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
            "{ barShort: { gte: 12 }}");
        var tester = CreateProviderTester(new FooNullableFilterInput());

        // act
        var func = tester.Build<FooNullable>(value);

        // assert
        var a = new FooNullable { BarShort = 11, };
        Assert.False(func(a));

        var b = new FooNullable { BarShort = 12, };
        Assert.True(func(b));

        var c = new FooNullable { BarShort = 13, };
        Assert.True(func(c));

        var d = new FooNullable { BarShort = null, };
        Assert.False(func(d));
    }

    [Fact]
    public void Create_NullableShortNotGreaterThanOrEquals_Expression()
    {
        // arrange
        var value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
            "{ barShort: { ngte: 12 }}");
        var tester = CreateProviderTester(new FooNullableFilterInput());

        // act
        var func = tester.Build<FooNullable>(value);

        // assert
        var a = new FooNullable { BarShort = 11, };
        Assert.True(func(a));

        var b = new FooNullable { BarShort = 12, };
        Assert.False(func(b));

        var c = new FooNullable { BarShort = 13, };
        Assert.False(func(c));

        var d = new FooNullable { BarShort = null, };
        Assert.True(func(d));
    }

    [Fact]
    public void Create_NullableShortLowerThan_Expression()
    {
        // arrange
        var value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
            "{ barShort: { lt: 12 }}");
        var tester = CreateProviderTester(new FooNullableFilterInput());

        // act
        var func = tester.Build<FooNullable>(value);

        // assert
        var a = new FooNullable { BarShort = 11, };
        Assert.True(func(a));

        var b = new FooNullable { BarShort = 12, };
        Assert.False(func(b));

        var c = new FooNullable { BarShort = 13, };
        Assert.False(func(c));

        var d = new FooNullable { BarShort = null, };
        Assert.False(func(d));
    }

    [Fact]
    public void Create_NullableShortNotLowerThan_Expression()
    {
        // arrange
        var value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
            "{ barShort: { nlt: 12 }}");
        var tester = CreateProviderTester(new FooNullableFilterInput());

        // act
        var func = tester.Build<FooNullable>(value);

        // assert
        var a = new FooNullable { BarShort = 11, };
        Assert.False(func(a));

        var b = new FooNullable { BarShort = 12, };
        Assert.True(func(b));

        var c = new FooNullable { BarShort = 13, };
        Assert.True(func(c));

        var d = new FooNullable { BarShort = null, };
        Assert.True(func(d));
    }

    [Fact]
    public void Create_NullableShortLowerThanOrEquals_Expression()
    {
        // arrange
        var value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
            "{ barShort: { lte: 12 }}");
        var tester = CreateProviderTester(new FooNullableFilterInput());

        // act
        var func = tester.Build<FooNullable>(value);

        // assert
        var a = new FooNullable { BarShort = 11, };
        Assert.True(func(a));

        var b = new FooNullable { BarShort = 12, };
        Assert.True(func(b));

        var c = new FooNullable { BarShort = 13, };
        Assert.False(func(c));

        var d = new FooNullable { BarShort = null, };
        Assert.False(func(d));
    }

    [Fact]
    public void Create_NullableShortNotLowerThanOrEquals_Expression()
    {
        // arrange
        var value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
            "{ barShort: { nlte: 12 }}");
        var tester = CreateProviderTester(new FooNullableFilterInput());

        // act
        var func = tester.Build<FooNullable>(value);

        // assert
        var a = new FooNullable { BarShort = 11, };
        Assert.False(func(a));

        var b = new FooNullable { BarShort = 12, };
        Assert.False(func(b));

        var c = new FooNullable { BarShort = 13, };
        Assert.True(func(c));

        var d = new FooNullable { BarShort = null, };
        Assert.True(func(d));
    }

    [Fact]
    public void Create_NullableShortIn_Expression()
    {
        // arrange
        var value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
            "{ barShort: { in: [13, 14] }}");
        var tester = CreateProviderTester(new FooNullableFilterInput());

        // act
        var func = tester.Build<FooNullable>(value);

        // assert
        var a = new FooNullable { BarShort = 13, };
        Assert.True(func(a));

        var b = new FooNullable { BarShort = 12, };
        Assert.False(func(b));

        var c = new FooNullable { BarShort = null, };
        Assert.False(func(c));
    }

    [Fact]
    public void Create_NullableShortNotIn_Expression()
    {
        // arrange
        var value = Utf8GraphQLParser.Syntax.ParseValueLiteral(
            "{ barShort: { nin: [13, 14] }}");
        var tester = CreateProviderTester(new FooNullableFilterInput());

        // act
        var func = tester.Build<FooNullable>(value);

        // assert
        var a = new FooNullable { BarShort = 12, };
        Assert.True(func(a));

        var b = new FooNullable { BarShort = 13, };
        Assert.False(func(b));

        var c = new FooNullable { BarShort = null, };
        Assert.True(func(c));
    }

    [Fact]
    public void Overwrite_Comparable_Filter_Type_With_Attribute()
    {
        // arrange
        // act
        var schema = CreateSchema(new FilterInputType<EntityWithTypeAttribute>());

        // assert
        schema.MatchSnapshot();
    }

    public class Foo
    {
        public short BarShort { get; set; }
        public int BarInt { get; set; }
        public long BarLong { get; set; }
        public float BarFloat { get; set; }
        public double BarDouble { get; set; }
        public decimal BarDecimal { get; set; }
    }

    public class FooNullable
    {
        public short? BarShort { get; set; }
    }

    public class FooFilterInput
        : FilterInputType<Foo>
    {
    }

    public class FooNullableFilterInput
        : FilterInputType<FooNullable>
    {
    }

    public class EntityWithTypeAttribute
    {
        [GraphQLType(typeof(IntType))]
        public short? BarShort { get; set; }
    }

    public class Entity
    {
        public short? BarShort { get; set; }
    }
}
