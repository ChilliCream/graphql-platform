using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Internal;
using HotChocolate.Utilities;
using static HotChocolate.Resolvers.Expressions.Parameters.ParameterExpressionBuilderHelpers;

#nullable enable

namespace HotChocolate.Resolvers.Expressions.Parameters
{
    internal class ScopedServiceParameterExpressionBuilder : IParameterExpressionBuilder
    {
        private static readonly PropertyInfo _contextData =
            ContextType.GetProperty(
                nameof(IResolverContext.ScopedContextData))!;
        private static readonly MethodInfo _getScopedState =
            typeof(ExpressionHelper).GetMethod(
                nameof(ExpressionHelper.GetScopedState))!;

        private static readonly MethodInfo _getScopedStateWithDefault =
            typeof(ExpressionHelper).GetMethod(
                nameof(ExpressionHelper.GetScopedStateWithDefault))!;

        public virtual ArgumentKind Kind => ArgumentKind.Service;

        public bool IsPure => false;

        public virtual bool CanHandle(ParameterInfo parameter)
            => parameter.IsDefined(typeof(ScopedStateAttribute));

        public Expression Build(ParameterInfo parameter, Expression context)
        {
            ConstantExpression key = Expression.Constant(
                parameter.ParameterType.FullName ?? parameter.ParameterType.Name,
                typeof(string));

            MemberExpression contextData = Expression.Property(context, _contextData);

            MethodInfo getScopedState =
                parameter.HasDefaultValue
                    ? _getScopedStateWithDefault.MakeGenericMethod(parameter.ParameterType)
                    : _getScopedState.MakeGenericMethod(parameter.ParameterType);

            return parameter.HasDefaultValue
                ? Expression.Call(
                    getScopedState,
                    contextData,
                    key,
                    Expression.Constant(true, typeof(bool)),
                    Expression.Constant(parameter.RawDefaultValue, parameter.ParameterType))
                : Expression.Call(
                    getScopedState,
                    contextData,
                    key,
                    Expression.Constant(
                        new NullableHelper(parameter.ParameterType)
                            .GetFlags(parameter).FirstOrDefault() ?? false,
                        typeof(bool)));
        }
    }
}
