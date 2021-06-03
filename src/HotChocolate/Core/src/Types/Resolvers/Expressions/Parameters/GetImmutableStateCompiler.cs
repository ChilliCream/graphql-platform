using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Utilities;

namespace HotChocolate.Resolvers.Expressions.Parameters
{
    internal abstract class GetImmutableStateCompiler<T>
        : ScopedStateCompilerBase<T>
        where T : IResolverContext
    {
        private static readonly MethodInfo _getScopedState =
            typeof(ExpressionHelper).GetMethod(
                nameof(ExpressionHelper.GetScopedState));

        private static readonly MethodInfo _getScopedStateWithDefault =
            typeof(ExpressionHelper).GetMethod(
                nameof(ExpressionHelper.GetScopedStateWithDefault));

        protected override Expression Compile(
            Expression context,
            ParameterInfo parameter,
            ConstantExpression key)
        {
            MemberExpression contextData =
                Expression.Property(context, GetStateProperty());

            MethodInfo getGlobalState =
                parameter.HasDefaultValue
                    ? _getScopedStateWithDefault.MakeGenericMethod(parameter.ParameterType)
                    : _getScopedState.MakeGenericMethod(parameter.ParameterType);

            return parameter.HasDefaultValue
                ? Expression.Call(
                    getGlobalState,
                    contextData,
                    key,
                    Expression.Constant(true, typeof(bool)),
                    Expression.Constant(parameter.RawDefaultValue, parameter.ParameterType))
                : Expression.Call(
                    getGlobalState,
                    contextData,
                    key,
                    Expression.Constant(
                        new NullableHelper(parameter.ParameterType)
                            .GetFlags(parameter).FirstOrDefault() ?? false,
                        typeof(bool)));
        }

        protected abstract PropertyInfo GetStateProperty();
    }
}
