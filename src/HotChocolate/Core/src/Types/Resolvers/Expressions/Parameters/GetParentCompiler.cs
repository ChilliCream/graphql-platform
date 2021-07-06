using System;
using System.Reflection;
using HotChocolate.Resolvers.CodeGeneration;

#nullable enable

namespace HotChocolate.Resolvers.Expressions.Parameters
{
    internal sealed class GetParentCompiler: GetFromGenericMethodCompilerBase
    {
        public GetParentCompiler()
            => GenericMethod = PureContextType.GetMethod(nameof(IPureResolverContext.Parent))!;


        public override bool CanHandle(ParameterInfo parameter, Type sourceType)
            => ArgumentHelper.IsParent(parameter, sourceType);
    }
}
