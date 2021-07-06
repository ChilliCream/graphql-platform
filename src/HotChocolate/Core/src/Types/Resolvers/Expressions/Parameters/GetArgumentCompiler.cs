using System;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Language;

#nullable enable

namespace HotChocolate.Resolvers.Expressions.Parameters
{
    internal sealed class GetArgumentCompiler : ResolverParameterCompilerBase
    {
        private readonly MethodInfo _argument;
        private readonly MethodInfo _argumentLiteral;
        private readonly MethodInfo _argumentOptional;
        private readonly Type _optional = typeof(Optional<>);

        public GetArgumentCompiler()
        {
            _argument = PureContextType.GetMethod(
                nameof(IPureResolverContext.ArgumentValue))!;
            _argumentLiteral = PureContextType.GetMethod(
                nameof(IPureResolverContext.ArgumentLiteral))!;
            _argumentOptional = PureContextType.GetMethod(
                nameof(IPureResolverContext.ArgumentOptional))!;
        }

        public override bool CanHandle(
            ParameterInfo parameter,
            Type sourceType)
            => true;

        public override Expression Compile(
            Expression context,
            ParameterInfo parameter,
            Type sourceType)
        {
            string name = parameter.IsDefined(typeof(GraphQLNameAttribute))
                ? parameter.GetCustomAttribute<GraphQLNameAttribute>()!.Name
                : parameter.Name!;

            MethodInfo argumentMethod;

            if (parameter.ParameterType.IsGenericType &&
                parameter.ParameterType.GetGenericTypeDefinition() == _optional)
            {
                argumentMethod = _argumentOptional.MakeGenericMethod(
                    parameter.ParameterType.GenericTypeArguments[0]);
            }
            else if (typeof(IValueNode).IsAssignableFrom(parameter.ParameterType))
            {
                argumentMethod = _argumentLiteral.MakeGenericMethod(
                    parameter.ParameterType);
            }
            else
            {
                argumentMethod = _argument.MakeGenericMethod(
                    parameter.ParameterType);
            }

            return Expression.Call(context, argumentMethod,
                Expression.Constant(new NameString(name)));
        }
    }
}
