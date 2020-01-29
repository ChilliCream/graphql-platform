using System.Threading.Tasks;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public class OutputModelGenerator
        : CodeGenerator<OutputModelDescriptor>
    {
        protected override Task WriteAsync(
            CodeWriter writer,
            OutputModelDescriptor descriptor)
        {
            ClassBuilder builder =
                ClassBuilder.New()
                    .SetAccessModifier(AccessModifier.Public)
                    .SetName(descriptor.Name);

            foreach(string typeName in descriptor.Implements)
            {
                builder.AddImplements(typeName);
            }

            foreach (OutputFieldDescriptor field in descriptor.Fields)
            {
                builder.AddProperty(
                    PropertyBuilder.New()
                        .SetAccessModifier(AccessModifier.Public)
                        .SetName(field.Name)
                        .SetType(field.Type));
            }

            return builder.BuildAsync(writer);
        }
    }
}
