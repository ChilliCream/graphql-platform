using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.CSharp.Extensions;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public class InputTypeGenerator : CodeGenerator<InputObjectTypeDescriptor>
    {
        protected override bool CanHandle(InputObjectTypeDescriptor descriptor) => true;

        protected override void Generate(
            CodeWriter writer,
            InputObjectTypeDescriptor namedTypeDescriptor,
            out string fileName)
        {
            fileName = namedTypeDescriptor.Name;
            ClassBuilder classBuilder = ClassBuilder.New()
                .SetName(fileName);

            foreach (var prop in namedTypeDescriptor.Properties)
            {
                TypeReferenceBuilder propTypeBuilder = prop.Type.ToBuilder();

                // Add Property to class
                PropertyBuilder propBuilder = PropertyBuilder
                    .New()
                    .MakeSettable()
                    .SetName(prop.Name)
                    .SetType(propTypeBuilder)
                    .SetAccessModifier(AccessModifier.Public);
                classBuilder.AddProperty(propBuilder);
            }

            /*
             TODO: I dont think this is needed?
            foreach (var implement in namedTypeDescriptor.Implements)
            {
                classBuilder.AddImplements(implement);
            }
            */

            CodeFileBuilder
                .New()
                .SetNamespace(namedTypeDescriptor.RuntimeType.Namespace)
                .AddType(classBuilder)
                .Build(writer);
        }
    }
}
