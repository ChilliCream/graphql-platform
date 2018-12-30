using System;
using HotChocolate.Utilities;

namespace HotChocolate.Resolvers.CodeGeneration
{
    internal sealed class DataLoaderArgumentSourceCodeGenerator
        : ArgumentSourceCodeGenerator
    {
        protected override ArgumentKind Kind => ArgumentKind.DataLoader;

        protected override string Generate(
            ArgumentDescriptor descriptor)
        {
            return $"ctx.DataLoader<{descriptor.Type.GetTypeName()}>()";
        }
    }
}
