using System;
using System.Linq.Expressions;
using System.Reflection;

namespace HotChocolate.Resolvers.Expressions.Parameters
{
    internal abstract class ScopedStateCompilerBase<T>
        : CustomContextCompilerBase<T>
        where T : IResolverContext
    {
        public override Expression Compile(
            Expression context,
            ParameterInfo parameter,
            Type sourceType)
        {
            string explicitKey = GetKey(parameter);

            ConstantExpression key =
                explicitKey is null
                    ? Expression.Constant(parameter.Name, typeof(string))
                    : Expression.Constant(explicitKey, typeof(string));

            return Compile(context, parameter, key);
        }

        protected abstract Expression Compile(
            Expression context,
            ParameterInfo parameter,
            ConstantExpression key);

        protected abstract string GetKey(ParameterInfo parameter);
    }
}
