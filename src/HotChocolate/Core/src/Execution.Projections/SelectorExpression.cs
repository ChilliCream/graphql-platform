using System.Linq.Expressions;

namespace HotChocolate.Execution.Projections;

internal abstract class SelectorExpression(
    Type valueType,
    ulong includeFlags,
    ulong conditionMask,
    LambdaExpression expression)
{
    public Type ValueType { get; } = valueType;

    public ulong IncludeFlags { get; } = includeFlags;

    public ulong ConditionMask { get; } = conditionMask;

    public LambdaExpression Expression { get; } = expression;
}

internal sealed class SelectorExpression<TValue>(
    ulong includeFlags,
    ulong conditionMask,
    Expression<Func<TValue, TValue>> expression)
    : SelectorExpression(typeof(TValue), includeFlags, conditionMask, expression)
{
    public new Expression<Func<TValue, TValue>> Expression
        => (Expression<Func<TValue, TValue>>)base.Expression;
}
