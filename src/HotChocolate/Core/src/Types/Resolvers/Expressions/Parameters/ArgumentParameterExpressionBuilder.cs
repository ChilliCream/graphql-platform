using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Internal;
using HotChocolate.Language;
using static HotChocolate.Resolvers.Expressions.Parameters.ParameterExpressionBuilderHelpers;

#nullable enable

namespace HotChocolate.Resolvers.Expressions.Parameters;

internal class ArgumentParameterExpressionBuilder
    : IParameterExpressionBuilder
    , IParameterBindingFactory
{
    private const string _argumentValue = nameof(IResolverContext.ArgumentValue);
    private const string _argumentLiteral = nameof(IResolverContext.ArgumentLiteral);
    private const string _argumentOptional = nameof(IResolverContext.ArgumentOptional);
    private static readonly Type _optional = typeof(Optional<>);

    private static readonly MethodInfo _getArgumentValue =
        ContextType.GetMethods().First(IsArgumentValueMethod);
    private static readonly MethodInfo _getArgumentLiteral =
        ContextType.GetMethods().First(IsArgumentLiteralMethod);
    private static readonly MethodInfo _getArgumentOptional =
        ContextType.GetMethods().First(IsArgumentOptionalMethod);

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

    public Expression Build(ParameterExpressionBuilderContext context)
    {
        var parameter = context.Parameter;
        var name = context.ArgumentName;

        if (name is null)
        {
            name = parameter.IsDefined(typeof(ArgumentAttribute))
                ? parameter.GetCustomAttribute<ArgumentAttribute>()!.Name ?? parameter.Name!
                : parameter.Name!;

            if (parameter.IsDefined(typeof(GraphQLNameAttribute)))
            {
                name = parameter.GetCustomAttribute<GraphQLNameAttribute>()!.Name;
            }
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

        return Expression.Call(context.ResolverContext, argumentMethod, Expression.Constant(name));
    }

    public IParameterBinding Create(ParameterBindingContext context)
        => new ArgumentBinding(context.ArgumentName);

    private sealed class ArgumentBinding(string name) : IParameterBinding
    {
        public ArgumentKind Kind => ArgumentKind.Argument;

        public bool IsPure => true;

        public T Execute<T>(IResolverContext context)
            => context.ArgumentValue<T>(name);
    }
}
