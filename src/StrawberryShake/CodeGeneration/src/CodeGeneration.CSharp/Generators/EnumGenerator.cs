using System.Threading.Tasks;
using StrawberryShake.CodeGeneration.CSharp.Builders;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public class EnumGenerator
        : CSharpBaseGenerator<EnumDescriptor>
    {
        protected override Task WriteAsync(CodeWriter writer, EnumDescriptor descriptor)
        {
            AssertNonNull(writer, descriptor);

            EnumBuilder enumBuilder = EnumBuilder.New()
                .SetName(descriptor.Name);

            foreach (EnumValueDescriptor element in descriptor.Values)
            {
                enumBuilder.AddElement(element.Name, element.Value);
            }

            return CodeFileBuilder.New()
                .SetNamespace(descriptor.Namespace)
                .AddType(enumBuilder)
                .BuildAsync(writer);
        }
    }
}
