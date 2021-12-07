using System;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Internal;
using HotChocolate.Types.Descriptors;

#nullable enable

namespace HotChocolate.Resolvers.Expressions.Parameters;

internal class ScopedServiceParameterExpressionBuilder
    : IParameterExpressionBuilder
    , IParameterFieldConfiguration
{
    public ArgumentKind Kind => ArgumentKind.Service;

    public bool IsPure => false;

    public bool IsDefaultHandler => false;

    public bool CanHandle(ParameterInfo parameter)
        => ServiceExpressionHelper.TryGetServiceKind(parameter, out ServiceKind kind) &&
           kind is not ServiceKind.Default;

    public void ApplyConfiguration(ParameterInfo parameter, ObjectFieldDescriptor descriptor)
    {
        ServiceExpressionHelper.TryGetServiceKind(parameter, out ServiceKind kind);
        ServiceExpressionHelper.ApplyConfiguration(parameter, descriptor, kind);
    }

    public Expression Build(ParameterInfo parameter, Expression context)
    {
        ServiceExpressionHelper.TryGetServiceKind(parameter, out ServiceKind kind);
        return ServiceExpressionHelper.Build(parameter, context, kind);
    }
}
