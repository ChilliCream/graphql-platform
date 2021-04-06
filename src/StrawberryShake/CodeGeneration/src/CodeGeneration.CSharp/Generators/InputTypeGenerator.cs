using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.CSharp.Extensions;
using StrawberryShake.CodeGeneration.Descriptors.TypeDescriptors;

namespace StrawberryShake.CodeGeneration.CSharp.Generators
{
    public class InputTypeGenerator : CodeGenerator<InputObjectTypeDescriptor>
    {
        protected override void Generate(InputObjectTypeDescriptor namedTypeDescriptor,
            CodeGeneratorSettings settings,
            CodeWriter writer,
            out string fileName,
            out string? path)
        {
            fileName = namedTypeDescriptor.Name;
            path = null;

            ClassBuilder classBuilder = ClassBuilder
                .New()
                .SetComment(namedTypeDescriptor.Documentation)
                .SetName(fileName);

            foreach (var prop in namedTypeDescriptor.Properties)
            {
                classBuilder
                    .AddProperty(prop.Name)
                    .SetPublic()
                    .SetComment(prop.Description)
                    .SetType(prop.Type.ToTypeReference())
                    .MakeSettable()
                    .SetValue("default!");
            }

            CodeFileBuilder
                .New()
                .SetNamespace(namedTypeDescriptor.RuntimeType.NamespaceWithoutGlobal)
                .AddType(classBuilder)
                .Build(writer);
        }
    }
}
