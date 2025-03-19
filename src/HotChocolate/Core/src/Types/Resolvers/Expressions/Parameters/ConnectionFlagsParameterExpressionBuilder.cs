using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Internal;
using HotChocolate.Types.Pagination;

namespace HotChocolate.Resolvers.Expressions.Parameters;

internal sealed class ConnectionFlagsParameterExpressionBuilder
    : IParameterExpressionBuilder
    , IParameterBindingFactory
    , IParameterBinding
{
    public ArgumentKind Kind => ArgumentKind.Custom;

    public bool IsPure => true;

    public bool IsDefaultHandler => false;

    public bool CanHandle(ParameterInfo parameter)
        => parameter.ParameterType == typeof(ConnectionFlags);

    public IParameterBinding Create(ParameterBindingContext context)
        => this;

    public Expression Build(ParameterExpressionBuilderContext context)
        => CreateInvokeExpression(context.ResolverContext, ctx => Execute(ctx));

    private InvocationExpression CreateInvokeExpression(
        Expression context,
        Expression<Func<IResolverContext, ConnectionFlags>> lambda)
        => Expression.Invoke(lambda, context);

    public T Execute<T>(IResolverContext context)
        => (T)(object)Execute(context);

    private static ConnectionFlags Execute(IResolverContext context)
    {
        var flags = ConnectionFlags.None;

        if (context.IsSelected("totalCount"))
        {
            flags |= ConnectionFlags.TotalCount;
        }

        if (context.IsSelected("edges"))
        {
            flags |= ConnectionFlags.Edges;
        }

        if (context.IsSelected("nodes"))
        {
            flags |= ConnectionFlags.Nodes;
        }

        return flags;
    }
}
