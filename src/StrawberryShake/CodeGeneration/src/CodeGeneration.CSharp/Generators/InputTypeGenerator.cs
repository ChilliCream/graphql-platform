using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.CSharp.Extensions;
using StrawberryShake.CodeGeneration.Extensions;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public class InputTypeGenerator: CodeGenerator<NamedTypeDescriptor>
    {
        protected override bool CanHandle(NamedTypeDescriptor descriptor)
        {
            return descriptor.Kind == TypeKind.InputType;
        }

        protected override void Generate(
            CodeWriter writer,
            NamedTypeDescriptor namedTypeDescriptor,
            out string fileName)
        {
            fileName = namedTypeDescriptor.Name;
            ClassBuilder classBuilder = ClassBuilder.New()
                .SetName(fileName);

            foreach (var prop in namedTypeDescriptor.Properties)
            {
                var propTypeBuilder = prop.Type.ToBuilder();

                // Add Property to class
                var propBuilder = PropertyBuilder
                    .New()
                    .MakeSettable()
                    .SetName(prop.Name)
                    .SetType(propTypeBuilder)
                    .SetAccessModifier(AccessModifier.Public);
                classBuilder.AddProperty(propBuilder);
            }

            foreach (var implement in namedTypeDescriptor.Implements)
            {
                classBuilder.AddImplements(implement);
            }

            CodeFileBuilder
                .New()
                .SetNamespace(namedTypeDescriptor.Namespace)
                .AddType(classBuilder)
                .Build(writer);
        }
    }
}
