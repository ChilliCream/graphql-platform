using System.Linq.Expressions;
using System.Reflection;

namespace HotChocolate.Utilities;

public sealed class TypeParameterHandler : IParameterHandler
{
    private readonly Type _parameterType;
    private readonly Expression _expression;

    public TypeParameterHandler(Type parameterType, Expression expression)
    {
        _parameterType = parameterType ??
            throw new ArgumentNullException(nameof(parameterType));
        _expression = expression ??
            throw new ArgumentNullException(nameof(expression));
    }

    public bool CanHandle(ParameterInfo parameter)
    {
        if (parameter is null)
        {
            throw new ArgumentNullException(nameof(parameter));
        }

        return parameter.ParameterType == _parameterType;
    }

    public Expression CreateExpression(ParameterInfo parameter) =>
        _expression;
}
