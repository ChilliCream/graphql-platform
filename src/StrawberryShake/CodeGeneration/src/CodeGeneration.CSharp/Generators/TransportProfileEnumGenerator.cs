using System.Linq;
using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.Descriptors;

namespace StrawberryShake.CodeGeneration.CSharp.Generators
{
    public class TransportProfileEnumGenerator : CodeGenerator<DependencyInjectionDescriptor>
    {
        protected override bool CanHandle(DependencyInjectionDescriptor descriptor)
        {
            return descriptor.TransportProfiles.Count > 1;
        }

        protected override void Generate(
            CodeWriter writer,
            DependencyInjectionDescriptor descriptor,
            out string fileName,
            out string? path)
        {
            fileName = NamingConventions.CreateClientProfileKind(descriptor.Name);
            path = null;

            CodeFileBuilder
                .New()
                .SetNamespace(descriptor.ClientDescriptor.RuntimeType.NamespaceWithoutGlobal)
                .AddType(EnumBuilder
                    .New()
                    .SetName(fileName)
                    .AddElements(descriptor.TransportProfiles.Select(x => x.Name)))
                .Build(writer);
        }
    }
}
