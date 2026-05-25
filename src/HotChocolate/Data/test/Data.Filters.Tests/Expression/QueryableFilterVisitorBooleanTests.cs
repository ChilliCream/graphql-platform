using System.Linq.Expressions;
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
        var a = new Foo { Bar = true };
        Assert.True(func(a));

        var b = new Foo { Bar = false };
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
        var a = new Foo { Bar = false };
        Assert.True(func(a));

        var b = new Foo { Bar = true };
        Assert.False(func(b));
    }

    [Fact]
    public void Create_BooleanEqual_Expression_True_Uses_Direct_Property()
    {
        // arrange
        var value = Utf8GraphQLParser.Syntax.ParseValueLiteral("{ bar: { eq: true }}");
        var tester = CreateProviderTester(new FooFilterInput());

        // act
        var expr = tester.BuildExpression<Foo>(value);

        // assert - the in-memory context wraps with NotNullAndAlso(x, condition),
        // so the body is AndAlso(x != null, condition). The right side should be
        // a direct property access (MemberExpression), not an Equal comparison.
        var andAlso = Assert.IsAssignableFrom<BinaryExpression>(expr.Body);
        Assert.Equal(ExpressionType.AndAlso, andAlso.NodeType);
        Assert.IsAssignableFrom<MemberExpression>(andAlso.Right);
    }

    [Fact]
    public void Create_BooleanEqual_Expression_False_Uses_Not()
    {
        // arrange
        var value = Utf8GraphQLParser.Syntax.ParseValueLiteral("{ bar: { eq: false }}");
        var tester = CreateProviderTester(new FooFilterInput());

        // act
        var expr = tester.BuildExpression<Foo>(value);

        // assert - right side of AndAlso should be Not(property)
        var andAlso = Assert.IsAssignableFrom<BinaryExpression>(expr.Body);
        Assert.Equal(ExpressionType.AndAlso, andAlso.NodeType);
        var not = Assert.IsType<UnaryExpression>(andAlso.Right);
        Assert.Equal(ExpressionType.Not, not.NodeType);
        Assert.IsAssignableFrom<MemberExpression>(not.Operand);
    }

    [Fact]
    public void Create_BooleanNotEqual_Expression_True_Uses_Not()
    {
        // arrange
        var value = Utf8GraphQLParser.Syntax.ParseValueLiteral("{ bar: { neq: true }}");
        var tester = CreateProviderTester(new FooFilterInput());

        // act
        var expr = tester.BuildExpression<Foo>(value);

        // assert - right side of AndAlso should be Not(property)
        var andAlso = Assert.IsAssignableFrom<BinaryExpression>(expr.Body);
        Assert.Equal(ExpressionType.AndAlso, andAlso.NodeType);
        var not = Assert.IsType<UnaryExpression>(andAlso.Right);
        Assert.Equal(ExpressionType.Not, not.NodeType);
        Assert.IsAssignableFrom<MemberExpression>(not.Operand);
    }

    [Fact]
    public void Create_BooleanNotEqual_Expression_False_Uses_Direct_Property()
    {
        // arrange
        var value = Utf8GraphQLParser.Syntax.ParseValueLiteral("{ bar: { neq: false }}");
        var tester = CreateProviderTester(new FooFilterInput());

        // act
        var expr = tester.BuildExpression<Foo>(value);

        // assert - right side of AndAlso should be direct property access
        var andAlso = Assert.IsAssignableFrom<BinaryExpression>(expr.Body);
        Assert.Equal(ExpressionType.AndAlso, andAlso.NodeType);
        Assert.IsAssignableFrom<MemberExpression>(andAlso.Right);
    }

    [Fact]
    public void Create_NullableBooleanEqual_Expression_Uses_Equality()
    {
        // arrange
        var value = Utf8GraphQLParser.Syntax.ParseValueLiteral("{ bar: { eq: true }}");
        var tester = CreateProviderTester(new FooNullableFilterInput());

        // act
        var expr = tester.BuildExpression<FooNullable>(value);

        // assert - nullable bool should still use Equal (not optimized).
        // The body is AndAlso(x != null, condition) where condition is Equal.
        var andAlso = Assert.IsAssignableFrom<BinaryExpression>(expr.Body);
        Assert.Equal(ExpressionType.AndAlso, andAlso.NodeType);
        Assert.Equal(ExpressionType.Equal, andAlso.Right.NodeType);
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
        var value = Utf8GraphQLParser.Syntax.ParseValueLiteral("{ bar: { eq: false }}");
        var tester = CreateProviderTester(new FooNullableFilterInput());

        // act
        var func = tester.Build<FooNullable>(value);

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

    public class FooFilterInput : FilterInputType<Foo>;

    public class FooNullableFilterInput : FilterInputType<FooNullable>;
}
