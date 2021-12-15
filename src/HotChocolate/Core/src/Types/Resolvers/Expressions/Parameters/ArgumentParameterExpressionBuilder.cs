using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Internal;
using HotChocolate.Language;
using static HotChocolate.Resolvers.Expressions.Parameters.ParameterExpressionBuilderHelpers;

#nullable enable

namespace HotChocolate.Resolvers.Expressions.Parameters;

internal class ArgumentParameterExpressionBuilder : IParameterExpressionBuilder
{
    private const string _argumentValue = nameof(IPureResolverContext.ArgumentValue);
    private const string _argumentLiteral = nameof(IPureResolverContext.ArgumentLiteral);
    private const string _argumentOptional = nameof(IPureResolverContext.ArgumentOptional);
    private static readonly Type _optional = typeof(Optional<>);

    private static readonly MethodInfo _getArgumentValue =
        PureContextType.GetMethods().First(IsArgumentValueMethod);
    private static readonly MethodInfo _getArgumentLiteral =
        PureContextType.GetMethods().First(IsArgumentLiteralMethod);
    private static readonly MethodInfo _getArgumentOptional =
        PureContextType.GetMethods().First(IsArgumentOptionalMethod);

    private static bool IsArgumentValueMethod(MethodInfo method)
        => method.Name.Equals(_argumentValue, StringComparison.Ordinal) &&
           method.IsGenericMethod;

    private static bool IsArgumentLiteralMethod(MethodInfo method)
        => method.Name.Equals(_argumentLiteral, StringComparison.Ordinal) &&
           method.IsGenericMethod;

    private static bool IsArgumentOptionalMethod(MethodInfo method)
        => method.Name.Equals(_argumentOptional, StringComparison.Ordinal) &&
           method.IsGenericMethod;

    public ArgumentKind Kind => ArgumentKind.Argument;

    public bool IsPure => true;

    public bool IsDefaultHandler => true;

    public virtual bool CanHandle(ParameterInfo parameter)
        => parameter.IsDefined(typeof(ArgumentAttribute));

    public Expression Build(ParameterInfo parameter, Expression context)
    {
        var name = parameter.IsDefined(typeof(ArgumentAttribute))
            ? parameter.GetCustomAttribute<ArgumentAttribute>()!.Name ?? parameter.Name!
            : parameter.Name!;

        if (parameter.IsDefined(typeof(GraphQLNameAttribute)))
        {
            name = parameter.GetCustomAttribute<GraphQLNameAttribute>()!.Name;
        }

        MethodInfo argumentMethod;

        if (parameter.ParameterType.IsGenericType &&
            parameter.ParameterType.GetGenericTypeDefinition() == _optional)
        {
            argumentMethod = _getArgumentOptional.MakeGenericMethod(
                parameter.ParameterType.GenericTypeArguments[0]);
        }
        else if (typeof(IValueNode).IsAssignableFrom(parameter.ParameterType))
        {
            argumentMethod = _getArgumentLiteral.MakeGenericMethod(
                parameter.ParameterType);
        }
        else
        {
            argumentMethod = _getArgumentValue.MakeGenericMethod(
                parameter.ParameterType);
        }

        return Expression.Call(context, argumentMethod,
            Expression.Constant(new NameString(name)));
    }
}
