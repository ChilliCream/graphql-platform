using StrawberryShake.CodeGeneration.CSharp.Builders;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public class EnumGenerator : CodeGenerator<EnumDescriptor>
    {
        protected override void Generate(
            CodeWriter writer,
            EnumDescriptor descriptor,
            out string fileName)
        {
            fileName = descriptor.Name;
            EnumBuilder enumBuilder =
                EnumBuilder
                    .New()
                    .SetName(fileName)
                    .SetUnderlyingType(descriptor.UnderlyingType);

            foreach (EnumValueDescriptor element in descriptor.Values)
            {
                enumBuilder.AddElement(element.Name, element.Value);
            }

            CodeFileBuilder
                .New()
                .SetNamespace(descriptor.Namespace)
                .AddType(enumBuilder)
                .Build(writer);
        }
    }
}
