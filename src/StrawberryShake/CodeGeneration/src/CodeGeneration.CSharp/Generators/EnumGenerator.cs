using System.Threading.Tasks;
using StrawberryShake.CodeGeneration.CSharp.Builders;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public class EnumGenerator : CodeGenerator<EnumDescriptor>
    {
        protected override void Generate(CodeWriter writer, EnumDescriptor descriptor)
        {
            EnumBuilder enumBuilder =
                EnumBuilder
                    .New()
                    .SetName(descriptor.Name);

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
