using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.CSharp.Extensions;
using StrawberryShake.CodeGeneration.Extensions;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public class ResultInterfaceGenerator: CodeGenerator<NamedTypeDescriptor>
    {
        protected override bool CanHandle(NamedTypeDescriptor descriptor)
        {
            return descriptor.IsInterface();
        }

        protected override void Generate(CodeWriter writer, NamedTypeDescriptor descriptor)
        {
            var interfaceBuilder = InterfaceBuilder.New()
                .SetName(descriptor.Name);

            foreach (var prop in descriptor.Properties)
            {
                var propTypeBuilder = prop.Type.ToBuilder();

                // Add Property to class
                var propBuilder = PropertyBuilder
                    .New()
                    .SetName(prop.Name)
                    .SetType(propTypeBuilder)
                    .SetAccessModifier(AccessModifier.Public);
                interfaceBuilder.AddProperty(propBuilder);
            }

            foreach (var implement in descriptor.Implements)
            {
                interfaceBuilder.AddImplements(implement);
            }

            CodeFileBuilder
                .New()
                .SetNamespace(descriptor.Namespace)
                .AddType(interfaceBuilder)
                .Build(writer);
        }
    }
}
