using System;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Resolvers.CodeGeneration;

#nullable enable

namespace HotChocolate.Resolvers.Expressions.Parameters
{
    internal sealed class SetLocalStateCompiler : ScopedStateCompilerBase
    {
        private static readonly MethodInfo _setScopedState =
            typeof(ExpressionHelper).GetMethod(
                nameof(ExpressionHelper.SetLocalState))!;

        private static readonly MethodInfo _setScopedStateGeneric =
            typeof(ExpressionHelper).GetMethod(
                nameof(ExpressionHelper.SetLocalStateGeneric))!;

        public override bool CanHandle(
            ParameterInfo parameter,
            Type sourceType)
            => ArgumentHelper.IsLocalState(parameter) &&
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

            return Expression.Call(
                setScopedState,
                context,
                key);
        }

        protected override string? GetKey(ParameterInfo parameter)
            => parameter.GetCustomAttribute<LocalStateAttribute>()?.Key;
    }
}
