using System;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Execution;
using HotChocolate.Resolvers.CodeGeneration;

namespace HotChocolate.Resolvers.Expressions.Parameters
{
    internal sealed class GetEventMessageCompiler<T>
        : ResolverParameterCompilerBase<T>
        where T : IResolverContext
    {
        private readonly MethodInfo _argument;

        public GetEventMessageCompiler()
        {
            _argument = ContextTypeInfo.GetDeclaredMethod(
                nameof(IResolverContext.CustomProperty));
        }

        public override bool CanHandle(
            ParameterInfo parameter,
            Type sourceType) =>
            ArgumentHelper.IsEventMessage(parameter);

        public override Expression Compile(
            Expression context,
            ParameterInfo parameter,
            Type sourceType)
        {
            MethodInfo argumentMethod = _argument.MakeGenericMethod(
                parameter.ParameterType);

            return Expression.Call(
                context,
                argumentMethod,
                Expression.Constant(WellKnownContextData.EventMessage));
        }
    }
}
