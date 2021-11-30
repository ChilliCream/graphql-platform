using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Internal;

namespace HotChocolate.Resolvers.Expressions.Parameters;

internal class LegacyScopedServiceParameterExpressionBuilder : IParameterExpressionBuilder
{
    public ArgumentKind Kind => ArgumentKind.Service;

    public bool IsPure => false;

    public bool CanHandle(ParameterInfo parameter)
        => parameter.ParameterType == typeof(ScopedServiceAttribute);

    public Expression Build(ParameterInfo parameter, Expression context)
    {
        ServiceExpressionHelper.TryGetServiceKind(parameter, out ServiceKind kind);
        return ServiceExpressionHelper.Build(parameter, context, kind);
    }
}
