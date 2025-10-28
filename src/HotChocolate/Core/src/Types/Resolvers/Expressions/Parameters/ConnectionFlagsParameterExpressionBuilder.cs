using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
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

    public bool CanHandle(ParameterDescriptor parameter)
        => parameter.Type == typeof(ConnectionFlags);

    public IParameterBinding Create(ParameterDescriptor parameter)
        => this;

    public Expression Build(ParameterExpressionBuilderContext context)
        => CreateInvokeExpression(context.ResolverContext, ctx => Execute(ctx));

    private InvocationExpression CreateInvokeExpression(
        Expression context,
        Expression<Func<IResolverContext, ConnectionFlags>> lambda)
        => Expression.Invoke(lambda, context);

    public T Execute<T>(IResolverContext context)
    {
        Debug.Assert(typeof(T) == typeof(ConnectionFlags));
        var flags = Execute(context);
        return Unsafe.As<ConnectionFlags, T>(ref flags);
    }

    private static ConnectionFlags Execute(IResolverContext context)
        => ConnectionFlagsHelper.GetConnectionFlags(context);
}
