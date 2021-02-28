using HotChocolate;
using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.CSharp.Extensions;
using StrawberryShake.CodeGeneration.Extensions;

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
            InterfaceBuilder interfaceBuilder =
                InterfaceBuilder.New().SetName(fileName);

            foreach (var prop in descriptor.Properties)
            {
                interfaceBuilder.AddProperty(
                    prop.Name,
                    x => x.SetType(prop.Type.ToBuilder()).SetAccessModifier(AccessModifier.Public));
            }

            foreach (NameString implement in descriptor.Implements)
            {
                interfaceBuilder.AddImplements(implement);
            }

            CodeFileBuilder
                .New()
                .SetNamespace(descriptor.RuntimeType.NamespaceWithoutGlobal)
                .AddType(interfaceBuilder)
                .Build(writer);
        }
    }
}
