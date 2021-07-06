using System;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Resolvers.CodeGeneration;

#nullable enable

namespace HotChocolate.Resolvers.Expressions.Parameters
{
    internal sealed class SetScopedStateCompiler : ScopedStateCompilerBase
    {
        private static readonly MethodInfo _setScopedState =
            typeof(ExpressionHelper).GetMethod(
                nameof(ExpressionHelper.SetScopedState))!;

        private static readonly MethodInfo _setScopedStateGeneric =
            typeof(ExpressionHelper).GetMethod(
                nameof(ExpressionHelper.SetScopedStateGeneric))!;

        public override bool CanHandle(
            ParameterInfo parameter,
            Type sourceType)
            => ArgumentHelper.IsScopedState(parameter) &&
               ArgumentHelper.IsStateSetter(parameter.ParameterType);

        protected override Expression Compile(
            Expression context,
            ParameterInfo parameter,
            ConstantExpression key)
        {
            MethodInfo setScopedState =
                parameter.ParameterType.IsGenericType
                    ? _setScopedStateGeneric.MakeGenericMethod(
                        parameter.ParameterType.GetGenericArguments()[0])
                    : _setScopedState;

            return Expression.Call(setScopedState, context, key);
        }

        protected override string? GetKey(ParameterInfo parameter)
            => parameter.GetCustomAttribute<ScopedStateAttribute>()?.Key;
    }
}
