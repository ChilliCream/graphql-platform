using StrawberryShake.CodeGeneration.CSharp.Builders;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public class EnumGenerator : CodeGenerator<EnumTypeDescriptor>
    {
        protected override void Generate(
            CodeWriter writer,
            EnumTypeDescriptor descriptor,
            out string fileName)
        {
            fileName = descriptor.Name;
            EnumBuilder enumBuilder =
                EnumBuilder
                    .New()
                    .SetName(descriptor.RuntimeType.Name)
                    .SetUnderlyingType(descriptor.UnderlyingType);

            foreach (EnumValueDescriptor element in descriptor.Values)
            {
                enumBuilder.AddElement(element.Name, element.Value);
            }

            CodeFileBuilder
                .New()
                .SetNamespace(descriptor.RuntimeType.NamespaceWithoutGlobal)
                .AddType(enumBuilder)
                .Build(writer);
        }
    }
}
