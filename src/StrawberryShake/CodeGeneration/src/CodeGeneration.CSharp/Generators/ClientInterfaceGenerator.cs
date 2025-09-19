using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.Descriptors;
using StrawberryShake.CodeGeneration.Utilities;

namespace StrawberryShake.CodeGeneration.CSharp.Generators;

public class ClientInterfaceGenerator : ClassBaseGenerator<ClientDescriptor>
{
    protected override void Generate(ClientDescriptor descriptor,
        CSharpSyntaxGeneratorSettings settings,
        CodeWriter writer,
        out string fileName,
        out string? path,
        out string ns)
    {
        fileName = descriptor.InterfaceType.Name;
        path = null;
        ns = descriptor.InterfaceType.NamespaceWithoutGlobal;

        var interfaceBuilder = InterfaceBuilder
            .New()
            .SetAccessModifier(settings.AccessModifier)
            .SetName(fileName)
            .SetComment(descriptor.Documentation);

        foreach (var operation in descriptor.Operations)
        {
            interfaceBuilder
                .AddProperty(NameUtils.GetPropertyName(operation.Name))
                .SetOnlyDeclaration()
                .SetType(operation.InterfaceType.ToString());
        }

        interfaceBuilder.Build(writer);
    }
}
