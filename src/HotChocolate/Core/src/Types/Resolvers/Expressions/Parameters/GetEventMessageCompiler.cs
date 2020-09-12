using System;
using System.Reflection;
using HotChocolate.Resolvers.CodeGeneration;

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
