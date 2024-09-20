using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.Descriptors.TypeDescriptors;

namespace StrawberryShake.CodeGeneration.CSharp.Generators;

public class EnumGenerator : CodeGenerator<EnumTypeDescriptor>
{
    protected override void Generate(EnumTypeDescriptor descriptor,
        CSharpSyntaxGeneratorSettings settings,
        CodeWriter writer,
        out string fileName,
        out string? path,
        out string ns)
    {
        fileName = descriptor.Name;
        path = null;
        ns = descriptor.RuntimeType.NamespaceWithoutGlobal;

        var enumBuilder = EnumBuilder
            .New()
            .SetAccessModifier(settings.AccessModifier)
            .SetComment(descriptor.Documentation)
            .SetName(descriptor.RuntimeType.Name)
            .SetUnderlyingType(descriptor.UnderlyingType);

        foreach (var element in descriptor.Values)
        {
            enumBuilder.AddElement(element.RuntimeValue, element.Value, element.Documentation);
        }

        enumBuilder.Build(writer);
    }
}
