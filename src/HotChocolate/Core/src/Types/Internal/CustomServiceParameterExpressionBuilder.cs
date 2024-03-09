using System;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Resolvers.Expressions.Parameters;
using HotChocolate.Types.Descriptors;

namespace HotChocolate.Internal;

/// <summary>
/// This expression builder allows to map custom services as resolver parameters that do
/// not need an attribute.
/// </summary>
public sealed class CustomServiceParameterExpressionBuilder<TService>(
    ServiceKind kind = ServiceKind.Default)
    : IParameterExpressionBuilder
    , IParameterFieldConfiguration
    where TService : class
{
    private static readonly Type _serviceType = typeof(TService);

    ArgumentKind IParameterExpressionBuilder.Kind
        => ArgumentKind.Service;

    bool IParameterExpressionBuilder.IsDefaultHandler => false;

    bool IParameterExpressionBuilder.IsPure
        => kind is ServiceKind.Default;

    public bool CanHandle(ParameterInfo parameter)
        => parameter.ParameterType == _serviceType;

    public void ApplyConfiguration(ParameterInfo parameter, ObjectFieldDescriptor descriptor)
        => ServiceExpressionHelper.ApplyConfiguration(parameter, descriptor, kind);

    public Expression Build(ParameterExpressionBuilderContext context)
        => ServiceExpressionHelper.Build(context.Parameter, context.ResolverContext, kind);
}
