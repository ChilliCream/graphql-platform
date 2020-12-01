using System;
using System.Threading.Tasks;
using StrawberryShake.CodeGeneration.CSharp.Builders;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public class InputModelGenerator
        : CodeGenerator<InputModelDescriptor>
    {
        protected override Task WriteAsync(
            CodeWriter writer,
            InputModelDescriptor descriptor)
        {
            if (writer is null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            if (descriptor is null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            ClassBuilder builder =
                ClassBuilder.New()
                    .SetAccessModifier(AccessModifier.Public)
                    .SetName(descriptor.Name);

            foreach (InputFieldDescriptor field in descriptor.Fields)
            {
                builder.AddProperty(
                    PropertyBuilder.New()
                        .SetAccessModifier(AccessModifier.Public)
                        .SetName(field.Name)
                        .SetType(field.Type)
                        .MakeSettable());
            }

            return builder.BuildAsync(writer);
        }
    }
}
