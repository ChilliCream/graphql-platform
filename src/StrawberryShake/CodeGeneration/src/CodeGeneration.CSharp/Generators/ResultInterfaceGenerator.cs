using System.Linq;
using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.CSharp.Extensions;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public class ResultInterfaceGenerator : CodeGenerator<InterfaceTypeDescriptor>
    {
        protected override bool CanHandle(InterfaceTypeDescriptor descriptor)
        {
            return true;
        }

        protected override void Generate(
            CodeWriter writer,
            InterfaceTypeDescriptor descriptor,
            out string fileName)
        {
            fileName = descriptor.RuntimeType.Name;

            InterfaceBuilder interfaceBuilder = InterfaceBuilder
                .New()
                .SetName(fileName);

            foreach (var prop in descriptor.Properties)
            {
                interfaceBuilder
                    .AddProperty(prop.Name)
                    .SetType(prop.Type.ToBuilder())
                    .SetPublic();
            }

            interfaceBuilder.AddImplementsRange(descriptor.Implements.Select(x => x.Value));

            CodeFileBuilder
                .New()
                .SetNamespace(descriptor.RuntimeType.NamespaceWithoutGlobal)
                .AddType(interfaceBuilder)
                .Build(writer);
        }
    }
}
