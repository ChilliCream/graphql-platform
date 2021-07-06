using System;
using System.Reflection;
using HotChocolate.Resolvers.CodeGeneration;

#nullable enable

namespace HotChocolate.Resolvers.Expressions.Parameters
{
    internal sealed class GetScopedStateCompiler : GetImmutableStateCompiler
    {
        public override bool CanHandle(
            ParameterInfo parameter,
            Type sourceType)
            => ArgumentHelper.IsScopedState(parameter) &&
               !ArgumentHelper.IsStateSetter(parameter.ParameterType);

        protected override PropertyInfo GetStateProperty()
            => ScopedContextData;

        protected override string? GetKey(ParameterInfo parameter)
            => parameter.GetCustomAttribute<ScopedStateAttribute>()?.Key;
    }
}
