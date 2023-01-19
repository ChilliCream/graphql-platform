using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Internal;

namespace HotChocolate.Resolvers.Expressions.Parameters;

internal sealed class LegacyScopedServiceParameterExpressionBuilder : IParameterExpressionBuilder
{
    public ArgumentKind Kind => ArgumentKind.Service;

    public bool IsPure => false;

    public bool IsDefaultHandler => false;

#pragma warning disable CS0618
    public bool CanHandle(ParameterInfo parameter)
        => parameter.IsDefined(typeof(ScopedServiceAttribute));
#pragma warning restore CS0618

    public Expression Build(ParameterInfo parameter, Expression context)
        => ServiceExpressionHelper.Build(parameter, context, ServiceKind.Pooled);
}
