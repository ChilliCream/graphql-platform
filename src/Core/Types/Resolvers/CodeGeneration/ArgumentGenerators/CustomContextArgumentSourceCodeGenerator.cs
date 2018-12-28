using System;
using HotChocolate.Utilities;

namespace HotChocolate.Resolvers.CodeGeneration
{
    internal sealed class CustomContextArgumentSourceCodeGenerator
        : ArgumentSourceCodeGenerator
    {
        protected override ArgumentKind Kind => ArgumentKind.CustomContext;

        protected override string Generate(
            ArgumentDescriptor descriptor)
        {
            // DataLoaderResolverContextExtensions.DataLoader<T>(context, key)
            //$"{nameof(DataLoaderResolverContextExtensions)}.{nameof(DataLoaderResolverContextExtensions.DataLoader)}<T>(context, key)"
            // return $"ctx.{nameof(IResolverContext.CustomContext)}<{descriptor.Type.GetTypeName()}>()";
            throw new NotImplementedException();
        }
    }
}
