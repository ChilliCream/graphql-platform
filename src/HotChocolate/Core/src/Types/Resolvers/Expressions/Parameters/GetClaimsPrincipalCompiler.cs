using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Security.Claims;
using HotChocolate.Utilities;

#nullable enable

namespace HotChocolate.Resolvers.Expressions.Parameters
{
    internal sealed class GetClaimsPrincipalCompiler<T>
        : CustomContextCompilerBase<T>
        where T : IResolverContext
    {
        private readonly MethodInfo _getGlobalState =
            typeof(ExpressionHelper).GetMethod(
                nameof(ExpressionHelper.GetGlobalState))!;

        public override bool CanHandle(ParameterInfo parameter, Type sourceType) =>
            parameter.ParameterType == typeof(ClaimsPrincipal);

        public override Expression Compile(
            Expression context,
            ParameterInfo parameter,
            Type sourceType)
        {
            ConstantExpression key = Expression.Constant(nameof(ClaimsPrincipal), typeof(string));
            MemberExpression contextData = Expression.Property(context, ContextData);

            return Expression.Call(
                _getGlobalState,
                contextData,
                key,
                Expression.Constant(
                    NullableHelper.IsParameterNullable(parameter),
                    typeof(bool)));
        }
    }
}
