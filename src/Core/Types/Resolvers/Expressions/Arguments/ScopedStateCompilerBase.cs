using System;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Resolvers.CodeGeneration;

namespace HotChocolate.Resolvers.Expressions.Parameters
{
    internal abstract class ScopedStateCompilerBase<T>
        : CustomContextCompilerBase<T>
        where T : IResolverContext
    {
        public override bool CanHandle(
            ParameterInfo parameter,
            Type sourceType)
        {
            return ArgumentHelper.IsScopedState(parameter)
                && CanHandle(parameter.ParameterType);
        }

        protected abstract bool CanHandle(Type parameterType);

        public override Expression Compile(
            Expression context,
            ParameterInfo parameter,
            Type sourceType)
        {
            ScopedStateAttribute attribute =
                parameter.GetCustomAttribute<ScopedStateAttribute>();

            ConstantExpression key =
                attribute.Key is null
                    ? Expression.Constant(parameter.Name, typeof(string))
                    : Expression.Constant(attribute.Key, typeof(string));

            return Compile(context, parameter, key);
        }

        protected abstract Expression Compile(
            Expression context,
            ParameterInfo parameter,
            ConstantExpression key);
    }
}
