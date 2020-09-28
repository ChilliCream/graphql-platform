using System;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Resolvers.CodeGeneration;

namespace HotChocolate.Resolvers.Expressions.Parameters
{
    internal abstract class GlobalStateCompilerBase<T>
        : CustomContextCompilerBase<T>
        where T : IResolverContext
    {
        public override bool CanHandle(
            ParameterInfo parameter,
            Type sourceType)
        {
            return ArgumentHelper.IsGlobalState(parameter)
                && CanHandle(parameter.ParameterType);
        }

        protected abstract bool CanHandle(Type parameterType);

        public override Expression Compile(
            Expression context,
            ParameterInfo parameter,
            Type sourceType)
        {
            GlobalStateAttribute attribute =
                parameter.GetCustomAttribute<GlobalStateAttribute>();

            ConstantExpression key =
                attribute.Key is null
                    ? Expression.Constant(parameter.Name, typeof(string))
                    : Expression.Constant(attribute.Key, typeof(string));

            MemberExpression contextData =
                Expression.Property(context, ContextData);

            return Compile(parameter, key, contextData);
        }

        protected abstract Expression Compile(
            ParameterInfo parameter,
            ConstantExpression key,
            MemberExpression contextData);
    }
}
