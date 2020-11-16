using System;
using System.Linq;
using System.Reflection;
using HotChocolate.Resolvers.CodeGeneration;

namespace HotChocolate.Resolvers.Expressions.Parameters
{
    internal sealed class GetServiceCompiler<T>
        : GetFromGenericMethodCompilerBase<T>
        where T : IResolverContext
    {
        public GetServiceCompiler()
        {
            GenericMethod = ContextTypeInfo.GetDeclaredMethods(
                nameof(IResolverContext.Service))
                .First(t => t.IsGenericMethod);
        }

        public override bool CanHandle(
            ParameterInfo parameter,
            Type sourceType) =>
            ArgumentHelper.IsService(parameter);
    }
}
