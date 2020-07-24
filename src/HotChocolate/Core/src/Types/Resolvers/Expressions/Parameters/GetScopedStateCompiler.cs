using System;
using System.Reflection;
using HotChocolate.Resolvers.CodeGeneration;

namespace HotChocolate.Resolvers.Expressions.Parameters
{
    internal sealed class GetScopedStateCompiler<T>
        : GetImmutableStateCompiler<T>
        where T : IResolverContext
    {
        public override bool CanHandle(
            ParameterInfo parameter,
            Type sourceType)
        {
            return ArgumentHelper.IsScopedState(parameter)
                && !IsSetter(parameter.ParameterType);
        }

        protected override PropertyInfo GetStateProperty() =>
            ScopedContextData;

        protected override string GetKey(ParameterInfo parameter) =>
            parameter.GetCustomAttribute<ScopedStateAttribute>().Key;
    }
}
