using System;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Subscriptions;

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
            typeof(IEventMessage).IsAssignableFrom(parameter.ParameterType);

        public override Expression Compile(
            ParameterInfo parameter,
            Type sourceType)
        {
            MethodInfo argumentMethod = _argument.MakeGenericMethod(
                parameter.ParameterType);

            return Expression.Call(Context, argumentMethod,
                Expression.Constant(typeof(IEventMessage).FullName));
        }
    }
}
