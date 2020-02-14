using System;
using System.Reflection;
using HotChocolate.Resolvers.CodeGeneration;

namespace HotChocolate.Resolvers.Expressions.Parameters
{
    internal sealed class GetLocalStateCompiler<T>
        : GetImmutableStateCompiler<T>
        where T : IResolverContext
    {
        public override bool CanHandle(
            ParameterInfo parameter,
            Type sourceType)
        {
            return ArgumentHelper.IsLocalState(parameter)
                && !IsSetter(parameter.ParameterType);
        }

        protected override PropertyInfo GetStateProperty() =>
            LocalContextData;

        protected override string GetKey(ParameterInfo parameter) =>
            parameter.GetCustomAttribute<LocalStateAttribute>().Key;
    }
}
