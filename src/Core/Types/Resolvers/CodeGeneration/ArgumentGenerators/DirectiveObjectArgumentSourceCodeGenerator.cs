using HotChocolate.Utilities;

namespace HotChocolate.Resolvers.CodeGeneration
{
    internal sealed class DirectiveObjectArgumentSourceCodeGenerator
        : ArgumentSourceCodeGenerator
    {
        protected override ArgumentKind Kind => ArgumentKind.DirectiveObject;

        protected override string Generate(ArgumentDescriptor descriptor)
        {
            return $"dir.ToObject<{descriptor.Type.GetTypeName()}>()";
        }
    }
}
