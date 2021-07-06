using System;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Resolvers.CodeGeneration;

#nullable enable

namespace HotChocolate.Resolvers.Expressions.Parameters
{
    internal sealed class SetGlobalStateCompiler : GlobalStateCompilerBase
    {
        private static readonly MethodInfo _setGlobalState =
            typeof(ExpressionHelper).GetMethod(nameof(ExpressionHelper.SetGlobalState))!;

        private static readonly MethodInfo _setGlobalStateGeneric =
            typeof(ExpressionHelper).GetMethod(nameof(ExpressionHelper.SetGlobalStateGeneric))!;

        protected override bool CanHandle(Type parameterType)
        {
            return ArgumentHelper.IsStateSetter(parameterType);
        }

        protected override Expression Compile(
            ParameterInfo parameter,
            ConstantExpression key,
            MemberExpression contextData)
        {
            MethodInfo setGlobalState =
                parameter.ParameterType.IsGenericType
                    ? _setGlobalStateGeneric.MakeGenericMethod(
                        parameter.ParameterType.GetGenericArguments()[0])
                    : _setGlobalState;

            return Expression.Call(
                setGlobalState,
                contextData,
                key);
        }
    }
}
