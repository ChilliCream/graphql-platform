using System;
using System.Reflection;
using HotChocolate.Resolvers.CodeGeneration;

namespace HotChocolate.Resolvers.Expressions.Parameters
{
    internal sealed class GetParentCompiler<T>
        : GetFromGenericMethodCompilerBase<T>
        where T : IResolverContext
    {
        public GetParentCompiler()
        {
            GenericMethod = ContextTypeInfo.GetDeclaredMethod(
                nameof(IResolverContext.Parent));
        }

        public override bool CanHandle(
            ParameterInfo parameter,
            Type sourceType) =>
            ArgumentHelper.IsParent(parameter, sourceType);
    }
}
