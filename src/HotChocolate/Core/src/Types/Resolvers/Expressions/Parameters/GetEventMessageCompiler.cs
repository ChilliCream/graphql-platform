using System;
using System.Linq.Expressions;
using System.Reflection;
using HotChocolate.Execution;
using HotChocolate.Resolvers.CodeGeneration;
using HotChocolate.Subscriptions;

namespace HotChocolate.Resolvers.Expressions.Parameters
{
    internal sealed class GetEventMessageCompiler<T>
        : GetImmutableStateCompiler<T>
        where T : IResolverContext
    {
        public override bool CanHandle(
            ParameterInfo parameter,
            Type sourceType) =>
            ArgumentHelper.IsEventMessage(parameter);

        protected override PropertyInfo GetStateProperty() =>
            ScopedContextData;

        protected override string GetKey(ParameterInfo parameter) =>
            WellKnownContextData.EventMessage;
    }
}
