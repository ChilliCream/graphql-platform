using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace HotChocolate.Data.ExpressionNodes;

public interface IBox
{
    PropertyInfo ValuePropertyInfo { get; }

    // Returns true if the value has changed.
    internal bool UpdateValue(object? newValue);

    internal IBox Clone();
}

public readonly record struct BoxExpressions(
    IBox Value,
    ConstantExpression ConstantBoxExpression,
    MemberExpression ValueExpression)
{
    public static BoxExpressions Create(IBox box)
    {
        var thisExpr = Expression.Constant(box);
        var valueExpr = Expression.Property(thisExpr, box.ValuePropertyInfo);
        return new BoxExpressions(box, thisExpr, valueExpr);
    }
}

public class Box<T> : IBox
{
    public T? Value { get; set; }
    public IEqualityComparer<T?> Comparer { get; set; } = EqualityComparer<T?>.Default;
    public PropertyInfo ValuePropertyInfo => Property;

    bool IBox.UpdateValue(object? newValue) => UpdateValue((T?) newValue);
    IBox IBox.Clone() => new Box<T> { Value = Value };

    internal bool UpdateValue(T? newValue)
    {
        if (Comparer.Equals(Value, newValue))
            return false;

        Value = newValue;
        return true;
    }


    public static readonly PropertyInfo Property =
        typeof(Box<T>).GetProperty(nameof(Value))!;
}

public static class BoxHelper
{
    private static readonly MethodInfo _createMethod = typeof(BoxHelper).GetMethod(nameof(CreateFromObject))!;
    private static readonly ConcurrentDictionary<Type, Func<object?, IBox>> _createFuncs = new();

    private static IBox CreateFromObject<T>(object? value)
        where T : IEquatable<T>
    {
        return new Box<T> { Value = (T?) value };
    }

    public static IBox Create(object? value, Type type)
    {
        var creator = _createFuncs.GetOrAdd(type,
            static type => _createMethod
                .MakeGenericMethod(type)
                .CreateDelegate<Func<object?, IBox>>());
        return creator(value);
    }

    public static Box<T> Create<T>(T value)
        where T : IEquatable<T>
    {
        return new Box<T> { Value = value };
    }
}
