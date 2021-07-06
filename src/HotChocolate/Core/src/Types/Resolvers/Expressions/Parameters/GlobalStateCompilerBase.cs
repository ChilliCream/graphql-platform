using System;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Resolvers.CodeGeneration;

#nullable enable

namespace HotChocolate.Resolvers.Expressions.Parameters
{
    internal abstract class GlobalStateCompilerBase : CustomContextCompilerBase
    {
        public override bool CanHandle(
            ParameterInfo parameter,
            Type sourceType)
            => ArgumentHelper.IsGlobalState(parameter) && 
               CanHandle(parameter.ParameterType);

        protected abstract bool CanHandle(Type parameterType);

        public override Expression Compile(
            Expression context,
            ParameterInfo parameter,
            Type sourceType)
        {
            GlobalStateAttribute attribute = parameter.GetCustomAttribute<GlobalStateAttribute>()!;

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
