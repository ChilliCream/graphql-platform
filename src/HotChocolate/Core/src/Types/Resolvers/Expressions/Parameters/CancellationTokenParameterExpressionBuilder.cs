using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Internal;
using static HotChocolate.Resolvers.Expressions.Parameters.ParameterExpressionBuilderHelpers;

#nullable enable

namespace HotChocolate.Resolvers.Expressions.Parameters;

internal sealed class CancellationTokenParameterExpressionBuilder
    : IParameterExpressionBuilder
    , IParameterBindingFactory
    , IParameterBinding
{
    private static readonly PropertyInfo _cancellationToken =
        ContextType.GetProperty(nameof(IResolverContext.RequestAborted))!;

    static CancellationTokenParameterExpressionBuilder()
    {
        Debug.Assert(_cancellationToken is not null, "RequestAborted property is missing.");
    }

    public ArgumentKind Kind => ArgumentKind.CancellationToken;

    public bool IsPure => false;

    public bool IsDefaultHandler => false;

    public bool CanHandle(ParameterInfo parameter)
        => typeof(CancellationToken) == parameter.ParameterType;

    public Expression Build(ParameterExpressionBuilderContext context)
        => Expression.Property(context.ResolverContext, _cancellationToken);

    public IParameterBinding Create(ParameterBindingContext context)
        => this;

    public T Execute<T>(IResolverContext context)
        => (T)(object)context.RequestAborted;
}
