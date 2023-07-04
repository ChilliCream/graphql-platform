using System;
using System.Linq.Expressions;
using System.Reflection;

namespace HotChocolate.Data.ExpressionNodes;

public interface IBox : IEquatable<IBox>
{
}

public readonly record struct BoxExpression(
    IBox Value,
    ConstantExpression ConstantBoxExpression,
    MemberExpression ValueExpression)
{
    public static BoxExpression Create<T>(Box<T> box)
        where T : IEquatable<T>
    {
        var thisExpr = Expression.Constant(box);
        var valueExpr = Expression.Property(thisExpr, Box<T>.Property);
        return new BoxExpression(box, thisExpr, valueExpr);
    }
}

public class Box<T> : IBox
    where T : IEquatable<T>
{
    public T? Value { get; set; }

    public static readonly PropertyInfo Property =
        typeof(Box<T>).GetProperty(nameof(Value))!;

    public bool Equals(IBox? other)
    {
        return other is Box<T> b && (Value is null ? b.Value is null : Value.Equals(b.Value));
    }
}
