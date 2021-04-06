using System.Linq;
using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.CSharp.Extensions;
using StrawberryShake.CodeGeneration.Descriptors.TypeDescriptors;

namespace StrawberryShake.CodeGeneration.CSharp.Generators
{
    public class ResultInterfaceGenerator : CodeGenerator<InterfaceTypeDescriptor>
    {
        protected override void Generate(InterfaceTypeDescriptor descriptor,
            CSharpSyntaxGeneratorSettings settings,
            CodeWriter writer,
            out string fileName,
            out string? path,
            out string ns)
        {
            fileName = descriptor.RuntimeType.Name;
            path = null;
            ns = descriptor.RuntimeType.NamespaceWithoutGlobal;

            InterfaceBuilder interfaceBuilder = InterfaceBuilder
                .New()
                .SetComment(descriptor.Description)
                .SetName(fileName);

            foreach (var prop in descriptor.Properties)
            {
                interfaceBuilder
                    .AddProperty(prop.Name)
                    .SetComment(prop.Description)
                    .SetType(prop.Type.ToTypeReference())
                    .SetPublic();
            }

            interfaceBuilder.AddImplementsRange(descriptor.Implements.Select(x => x.Value));
            interfaceBuilder.Build(writer);
        }
    }
}
