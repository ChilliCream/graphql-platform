using System;
using System.Reflection;
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
            string key = null;

            if (descriptor.Parameter != null
                && descriptor.Parameter.IsDefined(typeof(DataLoaderAttribute)))
            {
                key = WriteEscapeCharacters(descriptor.Parameter
                    .GetCustomAttribute<DataLoaderAttribute>().Key);
            }

            if (string.IsNullOrEmpty(key))
            {
                return $"ctx.DataLoader<{descriptor.Type.GetTypeName()}>()";
            }
            else
            {
                return $"ctx.DataLoader<{descriptor.Type.GetTypeName()}>(\"{key}\")";
            }
        }
    }
}
