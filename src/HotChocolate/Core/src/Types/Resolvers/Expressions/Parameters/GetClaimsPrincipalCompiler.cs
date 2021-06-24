using System;
using System.Linq.Expressions;
using System.Reflection;
using System.Security.Claims;
using HotChocolate.Utilities;
using static System.Linq.Expressions.Expression;
using static HotChocolate.Utilities.NullableHelper;

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
            ConstantExpression key = Constant(nameof(ClaimsPrincipal), typeof(string));
            MemberExpression contextData = Property(context, ContextData);
            MethodInfo globalState = _getGlobalState.MakeGenericMethod(parameter.ParameterType);
            Expression isNullable = Constant(IsParameterNullable(parameter), typeof(bool));
            return Call(globalState, contextData, key, isNullable);
        }
    }
}
