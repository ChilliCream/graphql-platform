using System;
using System.Reflection;
using HotChocolate.Resolvers.CodeGeneration;

#nullable enable

namespace HotChocolate.Resolvers.Expressions.Parameters
{
    internal sealed class GetLocalStateCompiler : GetImmutableStateCompiler
    {
        public override bool CanHandle(
            ParameterInfo parameter,
            Type sourceType)
            => ArgumentHelper.IsLocalState(parameter) && 
               !ArgumentHelper.IsStateSetter(parameter.ParameterType);

        protected override PropertyInfo GetStateProperty() =>
            LocalContextData;

        protected override string? GetKey(ParameterInfo parameter)
            => parameter.GetCustomAttribute<LocalStateAttribute>()?.Key;
    }
}
