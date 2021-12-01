using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Internal;

namespace HotChocolate.Resolvers.Expressions.Parameters;

internal class LegacyScopedServiceParameterExpressionBuilder : IParameterExpressionBuilder
{
    public ArgumentKind Kind => ArgumentKind.Service;

    public bool IsPure => false;

    public bool IsDefaultHandler => false;

    public bool CanHandle(ParameterInfo parameter)
        => parameter.IsDefined(typeof(ScopedServiceAttribute));

    public Expression Build(ParameterInfo parameter, Expression context)
        => ServiceExpressionHelper.Build(parameter, context, ServiceKind.Pooled);
}
