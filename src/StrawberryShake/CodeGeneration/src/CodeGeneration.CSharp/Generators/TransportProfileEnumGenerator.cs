using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.Descriptors;

namespace StrawberryShake.CodeGeneration.CSharp.Generators;

public class TransportProfileEnumGenerator : CodeGenerator<DependencyInjectionDescriptor>
{
    protected override bool CanHandle(DependencyInjectionDescriptor descriptor,
        CSharpSyntaxGeneratorSettings settings)
    {
        return descriptor.TransportProfiles.Count > 1;
    }

    protected override void Generate(DependencyInjectionDescriptor descriptor,
        CSharpSyntaxGeneratorSettings settings,
        CodeWriter writer,
        out string fileName,
        out string? path,
        out string ns)
    {
        fileName = NamingConventions.CreateClientProfileKind(descriptor.Name);
        path = null;
        ns = descriptor.ClientDescriptor.RuntimeType.NamespaceWithoutGlobal;

        EnumBuilder
            .New()
            .SetAccessModifier(settings.AccessModifier)
            .SetName(fileName)
            .AddElements(descriptor.TransportProfiles.Select(x => x.Name))
            .Build(writer);
    }
}
