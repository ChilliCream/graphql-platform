using System;
using System.Threading.Tasks;
using StrawberryShake.CodeGeneration.CSharp.Builders;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public class EnumGenerator
        : CodeGenerator<EnumDescriptor>
    {
        protected override Task WriteAsync(CodeWriter writer, EnumDescriptor descriptor)
        {
            if (writer is null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            if (descriptor is null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            EnumBuilder builder = EnumBuilder.New()
                .SetName(descriptor.Name);

            foreach (EnumElementDescriptor element in descriptor.Elements)
            {
                builder.AddElement(element.Name, element.Value);
            }

            return builder.BuildAsync(writer);
        }
    }
}
