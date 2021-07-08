using System;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Internal;
using HotChocolate.Language;
using static HotChocolate.Resolvers.Expressions.Parameters.ParameterExpressionBuilderHelpers;

#nullable enable

namespace HotChocolate.Resolvers.Expressions.Parameters
{
    internal class ArgumentParameterExpressionBuilder : IParameterExpressionBuilder
    {
        private const string _argumentValue = nameof(IPureResolverContext.ArgumentValue);
        private const string _argumentLiteral = nameof(IPureResolverContext.ArgumentLiteral);
        private const string _argumentOptional = nameof(IPureResolverContext.ArgumentOptional);
        private static readonly Type _optional = typeof(Optional<>);
        private static readonly MethodInfo _getArgumentValue;
        private static readonly MethodInfo _getArgumentLiteral;
        private static readonly MethodInfo _getArgumentOptional;

        static ArgumentParameterExpressionBuilder()
        {
            _getArgumentValue = PureContextType.GetMethods().First(IsArgumentValueMethod);
            Debug.Assert(_getArgumentValue is not null!, "ArgumentValue method is missing." );

            _getArgumentLiteral = PureContextType.GetMethods().First(IsArgumentLiteralMethod);
            Debug.Assert(_getArgumentValue is not null!, "ArgumentLiteral method is missing." );

            _getArgumentOptional = PureContextType.GetMethods().First(IsArgumentOptionalMethod);
            Debug.Assert(_getArgumentValue is not null!, "ArgumentOptional method is missing." );

            static bool IsArgumentValueMethod(MethodInfo method)
                => method.Name.Equals(_argumentValue, StringComparison.Ordinal) &&
                   method.IsGenericMethod;

            static bool IsArgumentLiteralMethod(MethodInfo method)
                => method.Name.Equals(_argumentLiteral, StringComparison.Ordinal) &&
                   method.IsGenericMethod;

            static bool IsArgumentOptionalMethod(MethodInfo method)
                => method.Name.Equals(_argumentOptional, StringComparison.Ordinal) &&
                   method.IsGenericMethod;
        }

        public ArgumentKind Kind => ArgumentKind.Argument;

        public bool IsPure => true;

        public virtual bool CanHandle(ParameterInfo parameter)
            => parameter.IsDefined(typeof(ArgumentAttribute));

        public Expression Build(ParameterInfo parameter, Expression context)
        {
            string name = parameter.IsDefined(typeof(ArgumentAttribute))
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
}
