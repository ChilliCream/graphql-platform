using System;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Resolvers.CodeGeneration;

namespace HotChocolate.Resolvers.Expressions.Parameters
{
    [Obsolete]
    internal sealed class GetCustomContextCompiler<T>
        : CustomContextCompilerBase<T>
        where T : IResolverContext
    {
        private static readonly MethodInfo _resolveContextData =
            typeof(ExpressionHelper).GetMethod(nameof(ExpressionHelper.ResolveContextData));
        private static readonly MethodInfo _resolveScopedContextData =
            typeof(ExpressionHelper).GetMethod(nameof(ExpressionHelper.ResolveScopedContextData));

        public override bool CanHandle(
            ParameterInfo parameter,
            Type sourceType) =>
            ArgumentHelper.IsState(parameter);

        public override Expression Compile(
            Expression context,
            ParameterInfo parameter,
            Type sourceType)
        {
            StateAttribute attribute =
                parameter.GetCustomAttribute<StateAttribute>();

            ConstantExpression key =
                Expression.Constant(attribute.Key);

            ConstantExpression defaultIfNotExists =
                Expression.Constant(attribute.DefaultIfNotExists);

            MemberExpression contextData = attribute.IsScoped
                ? Expression.Property(context, ScopedContextData)
                : Expression.Property(context, ContextData);

            MethodInfo resolveContextData = attribute.IsScoped
                ? _resolveScopedContextData.MakeGenericMethod(
                    parameter.ParameterType)
                : _resolveContextData.MakeGenericMethod(
                    parameter.ParameterType);

            return Expression.Call(
                resolveContextData,
                contextData,
                key,
                defaultIfNotExists);
        }
    }
}
