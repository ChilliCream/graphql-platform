using System;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Resolvers.CodeGeneration;

namespace HotChocolate.Resolvers.Expressions.Parameters
{
    internal sealed class GetCustomContextCompiler<T>
        : ResolverParameterCompilerBase<T>
        where T : IResolverContext
    {
        private static readonly MethodInfo _resolveContextData =
            typeof(ExpressionHelper).GetMethod("ResolveContextData");
        private static readonly MethodInfo _resolveScopedContextData =
            typeof(ExpressionHelper).GetMethod("ResolveScopedContextData");

        private readonly PropertyInfo _contextData;
        private readonly PropertyInfo _scopedContextData;

        public GetCustomContextCompiler()
        {
            _contextData = typeof(IHasContextData)
                .GetTypeInfo().GetDeclaredProperty(
                    nameof(IResolverContext.ContextData));
            _scopedContextData = ContextTypeInfo.GetDeclaredProperty(
                nameof(IResolverContext.ScopedContextData));
        }

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
                ? Expression.Property(context, _scopedContextData)
                : Expression.Property(context, _contextData);

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
