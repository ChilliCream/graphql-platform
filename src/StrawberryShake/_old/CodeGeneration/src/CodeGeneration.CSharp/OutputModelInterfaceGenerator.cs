using System;
using System.Threading.Tasks;
using StrawberryShake.CodeGeneration.CSharp.Builders;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public class OutputModelInterfaceGenerator
        : CodeGenerator<OutputModelInterfaceDescriptor>
    {
        protected override Task WriteAsync(
            CodeWriter writer,
            OutputModelInterfaceDescriptor descriptor)
        {
            if (writer is null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            if (descriptor is null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            InterfaceBuilder builder =
                InterfaceBuilder.New()
                    .SetAccessModifier(AccessModifier.Public)
                    .SetName(descriptor.Name);

            foreach(var typeName in descriptor.Implements)
            {
                builder.AddImplements(typeName);
            }

            foreach (OutputFieldDescriptor field in descriptor.Fields)
            {
                builder.AddProperty(
                    InterfacePropertyBuilder.New()
                        .SetName(field.Name)
                        .SetType(field.Type));
            }

            return builder.BuildAsync(writer);
        }
    }
}
