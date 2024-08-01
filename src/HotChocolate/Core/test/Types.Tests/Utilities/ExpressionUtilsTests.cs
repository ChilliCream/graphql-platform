using System.Linq.Expressions;
using System.Reflection;

namespace HotChocolate.Utilities;

public class ExpressionUtilsTests
{
    [Fact]
    public void PublicFieldExpression_ShouldThrow()
    {
        // act
        Action a = () => GetMember(t => t.Field);

        // assert
        Assert.Throws<ArgumentException>(a);
    }

    [Fact]
    public void PublicPropertyExpression_ShouldReturnProperty()
    {
        // act
        var member = GetMember(t => t.Property);

        // assert
        Assert.NotNull(member);
        Assert.Equal("Property", member.Name);
    }

    [Fact]
    public void PublicMethodExpression_ShouldReturnMethod()
    {
        // act
        var member = GetMember(t => t.Method());

        // assert
        Assert.NotNull(member);
        Assert.Equal("Method", member.Name);
    }

    [Fact]
    public void IndexerExpression_ShouldThrow()
    {
        // act
        void Action() => GetMember(t => t[default]);

        // assert
        Assert.Throws<ArgumentException>(Action);
    }

    public static MemberInfo GetMember<V>(Expression<Func<ExpressionUtilsTestDummy, V>> expr)
    {
        return expr.ExtractMember();
    }
}

public class ExpressionUtilsTestDummy
{
    public string Field;
    public string this[int index] { get => null; }
    public string Property { get; private set; }
    internal string InternalProperty { get; private set; }
    public string Method() => Property;
    internal string InternalMethod() => InternalProperty;
}
