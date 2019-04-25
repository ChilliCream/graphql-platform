using System.Reflection;
using HotChocolate.Utilities;

namespace HotChocolate.Resolvers.CodeGeneration
{
    internal sealed class DirectiveArgumentArgumentSourceCodeGenerator
        : ArgumentSourceCodeGenerator
    {
        protected override ArgumentKind Kind => ArgumentKind.DirectiveArgument;

        protected override string Generate(ArgumentDescriptor descriptor)
        {
            string name = WriteEscapeCharacters(descriptor.Name);

            if (descriptor.Parameter != null
                && descriptor.Parameter.IsDefined(typeof(DirectiveArgumentAttribute)))
            {
                DirectiveArgumentAttribute attribute = descriptor.Parameter
                    .GetCustomAttribute<DirectiveArgumentAttribute>();
                if (attribute.Name != null)
                {
                    name = WriteEscapeCharacters(attribute.Name);
                }
            }

            return $"dir.GetArgument<{descriptor.Type.GetTypeName()}>(\"{descriptor.Name}\")";
        }
    }
}
