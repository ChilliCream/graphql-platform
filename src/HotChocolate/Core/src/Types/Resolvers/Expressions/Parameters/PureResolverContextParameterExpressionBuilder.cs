using System;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Internal;

#nullable enable

namespace HotChocolate.Resolvers.Expressions.Parameters;

internal sealed class PureResolverContextParameterExpressionBuilder
    : IParameterExpressionBuilder
    , IParameterBindingFactory
    , IParameterBinding
{
    public ArgumentKind Kind => ArgumentKind.Context;

    public bool IsPure => true;

    public bool IsDefaultHandler => false;

    public bool CanHandle(ParameterInfo parameter)
        => typeof(IPureResolverContext) == parameter.ParameterType;

    public Expression Build(ParameterExpressionBuilderContext context)
        => context.ResolverContext;

    public IParameterBinding Create(ParameterBindingContext context)
        => this;

    public T Execute<T>(IResolverContext context)
        => throw new NotSupportedException();

    public T Execute<T>(IPureResolverContext context)
        => (T)(object)context;
}
