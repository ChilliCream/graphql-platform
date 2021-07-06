using System;
using System.Reflection;
using HotChocolate.Resolvers.CodeGeneration;

#nullable enable

namespace HotChocolate.Resolvers.Expressions.Parameters
{
    internal sealed class GetEventMessageCompiler : GetImmutableStateCompiler
    {
        public override bool CanHandle(
            ParameterInfo parameter,
            Type sourceType)
            => ArgumentHelper.IsEventMessage(parameter);

        protected override PropertyInfo GetStateProperty()
            => ScopedContextData;

        protected override string GetKey(ParameterInfo parameter)
            => WellKnownContextData.EventMessage;
    }
}
