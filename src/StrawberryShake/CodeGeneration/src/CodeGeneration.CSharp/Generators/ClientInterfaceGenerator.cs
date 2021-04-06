using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.Descriptors;
using StrawberryShake.CodeGeneration.Descriptors.Operations;
using StrawberryShake.CodeGeneration.Utilities;

namespace StrawberryShake.CodeGeneration.CSharp.Generators
{
    public class ClientInterfaceGenerator : ClassBaseGenerator<ClientDescriptor>
    {
        protected override void Generate(ClientDescriptor descriptor,
            CodeGeneratorSettings settings,
            CodeWriter writer,
            out string fileName,
            out string? path)
        {
            fileName = descriptor.InterfaceType.Name;
            path = null;

            InterfaceBuilder interfaceBuilder = InterfaceBuilder
                .New()
                .SetName(fileName)
                .SetComment(descriptor.Documentation);

            foreach (OperationDescriptor operation in descriptor.Operations)
            {
                interfaceBuilder
                    .AddProperty(NameUtils.GetPropertyName(operation.Name))
                    .SetOnlyDeclaration()
                    .SetType(operation.InterfaceType.ToString());
            }

            CodeFileBuilder
                .New()
                .SetNamespace(descriptor.InterfaceType.NamespaceWithoutGlobal)
                .AddType(interfaceBuilder)
                .Build(writer);
        }
    }
}
