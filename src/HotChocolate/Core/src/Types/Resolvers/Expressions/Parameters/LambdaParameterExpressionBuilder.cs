using System;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Internal;

#nullable enable

namespace HotChocolate.Resolvers.Expressions.Parameters;

/// <summary>
/// This base class allows to specify the argument expression as lambda expression
/// </summary>
internal abstract class LambdaParameterExpressionBuilder<TContext, TValue>
    : IParameterExpressionBuilder
    where TContext : IPureResolverContext
{
    private readonly Expression<Func<TContext, TValue>> _expression;

    protected LambdaParameterExpressionBuilder(Expression<Func<TContext, TValue>> expression)
    {
        _expression = expression;
        IsPure = typeof(TContext) == typeof(IPureResolverContext);
    }

    public abstract ArgumentKind Kind { get; }

    public bool IsPure { get; }

    public bool IsDefaultHandler => false;

    public abstract bool CanHandle(ParameterInfo parameter);

    public virtual Expression Build(ParameterExpressionBuilderContext context)
        => CreateInvokeExpression(context.ResolverContext);

    private InvocationExpression CreateInvokeExpression(Expression context)
        => Expression.Invoke(_expression, context);
}
