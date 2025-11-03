using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using HotChocolate.Internal;

namespace HotChocolate.Resolvers.Expressions.Parameters;

internal sealed class ResolverContextParameterExpressionBuilder
    : IParameterExpressionBuilder
    , IParameterBindingFactory
    , IParameterBinding
{
    public ArgumentKind Kind => ArgumentKind.Context;

    public bool IsPure => false;

    public bool IsDefaultHandler => false;

    public bool CanHandle(ParameterInfo parameter)
        => typeof(IResolverContext) == parameter.ParameterType;

    public bool CanHandle(ParameterDescriptor parameter)
        => typeof(IResolverContext) == parameter.Type;

    public Expression Build(ParameterExpressionBuilderContext context)
        => context.ResolverContext;

    public IParameterBinding Create(ParameterDescriptor parameter)
        => this;

    public T Execute<T>(IResolverContext context)
    {
        Debug.Assert(typeof(T) == typeof(IResolverContext));
        var ctx = context;
        return Unsafe.As<IResolverContext, T>(ref ctx);
    }
}
