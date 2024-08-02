using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Internal;

#nullable enable

namespace HotChocolate.Resolvers.Expressions.Parameters;

/// <summary>
/// This base class allows to specify the argument expression as lambda expression
/// </summary>
internal abstract class LambdaParameterExpressionBuilder<TValue>(
    Expression<Func<IResolverContext, TValue>> expression,
    bool isPure)
    : IParameterExpressionBuilder
{
    public abstract ArgumentKind Kind { get; }

    public bool IsPure { get; } = isPure;

    public bool IsDefaultHandler => false;

    public abstract bool CanHandle(ParameterInfo parameter);

    public virtual Expression Build(ParameterExpressionBuilderContext context)
        => CreateInvokeExpression(context.ResolverContext);

    private InvocationExpression CreateInvokeExpression(Expression context)
        => Expression.Invoke(expression, context);
}
