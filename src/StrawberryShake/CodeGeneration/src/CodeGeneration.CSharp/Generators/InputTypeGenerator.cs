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
                    .SetType(prop.Type.ToBuilder())
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
