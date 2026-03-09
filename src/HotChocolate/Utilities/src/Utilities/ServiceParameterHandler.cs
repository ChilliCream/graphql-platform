using System.Linq.Expressions;
using System.Reflection;

namespace HotChocolate.Utilities;

public sealed class ServiceParameterHandler : IParameterHandler
{
    private static readonly MethodInfo s_getService =
        typeof(IServiceProvider).GetMethod(nameof(IServiceProvider.GetService))!;
    private readonly Expression _services;

    public ServiceParameterHandler(Expression services)
    {
        _services = services;
    }

    public bool CanHandle(ParameterInfo parameter) => true;

    public Expression CreateExpression(ParameterInfo parameter)
    {
        ArgumentNullException.ThrowIfNull(parameter);

        return Expression.Convert(Expression.Call(
            _services,
            s_getService,
            Expression.Constant(parameter.ParameterType)),
            parameter.ParameterType);
    }
}
