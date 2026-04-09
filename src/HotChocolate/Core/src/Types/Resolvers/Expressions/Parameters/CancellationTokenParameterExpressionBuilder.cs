using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using HotChocolate.Internal;
using static HotChocolate.Resolvers.Expressions.Parameters.ParameterExpressionBuilderHelpers;

namespace HotChocolate.Resolvers.Expressions.Parameters;

internal sealed class CancellationTokenParameterExpressionBuilder
    : IParameterExpressionBuilder
    , IParameterBindingFactory
    , IParameterBinding
{
    private static readonly PropertyInfo s_cancellationToken =
        ContextType.GetProperty(nameof(IResolverContext.RequestAborted))!;

    static CancellationTokenParameterExpressionBuilder()
    {
        Debug.Assert(s_cancellationToken is not null, "RequestAborted property is missing.");
    }

    public ArgumentKind Kind => ArgumentKind.CancellationToken;

    public bool IsPure => false;

    public bool IsDefaultHandler => false;

    public bool CanHandle(ParameterInfo parameter)
        => typeof(CancellationToken) == parameter.ParameterType;

    public bool CanHandle(ParameterDescriptor parameter)
        => typeof(CancellationToken) == parameter.Type;

    public Expression Build(ParameterExpressionBuilderContext context)
        => Expression.Property(context.ResolverContext, s_cancellationToken);

    public IParameterBinding Create(ParameterDescriptor parameter)
        => this;

    public T Execute<T>(IResolverContext context)
    {
        Debug.Assert(typeof(T) == typeof(CancellationToken));
        var ct = context.RequestAborted;
        return Unsafe.As<CancellationToken, T>(ref ct);
    }
}
