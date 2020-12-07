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

            InterfaceBuilder classBuilder =
                InterfaceBuilder.New()
                    .SetAccessModifier(AccessModifier.Public)
                    .SetName(descriptor.Name);

            foreach (var typeName in descriptor.Implements)
            {
                classBuilder.AddImplements(typeName);
            }

            foreach (OutputFieldDescriptor field in descriptor.Fields)
            {
                classBuilder.AddProperty(
                    InterfacePropertyBuilder.New()
                        .SetName(field.Name)
                        .SetType(field.Type));
            }

            return CodeFileBuilder.New()
                .SetNamespace(descriptor.Namespace)
                .AddType(classBuilder)
                .BuildAsync(writer);
        }
    }
}
