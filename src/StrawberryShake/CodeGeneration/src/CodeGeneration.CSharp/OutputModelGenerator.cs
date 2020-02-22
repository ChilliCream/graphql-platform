using System;
using System.Text;
using System.Threading.Tasks;
using StrawberryShake.CodeGeneration.CSharp.Builders;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public class OutputModelGenerator
        : CodeGenerator<OutputModelDescriptor>
    {
        protected override Task WriteAsync(
            CodeWriter writer,
            OutputModelDescriptor descriptor)
        {
            if (writer is null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            if (descriptor is null)
            {
                throw new ArgumentNullException(nameof(descriptor));
            }

            ClassBuilder classBuilder =
                ClassBuilder.New()
                    .SetAccessModifier(AccessModifier.Public)
                    .SetName(descriptor.Name);

            ConstructorBuilder constructorBuilder =
                ConstructorBuilder.New()
                    .AddCode(CreateConstructorBody(descriptor));
            classBuilder.AddConstructor(constructorBuilder);

            foreach (var typeName in descriptor.Implements)
            {
                classBuilder.AddImplements(typeName);
            }

            foreach (OutputFieldDescriptor field in descriptor.Fields)
            {
                classBuilder.AddProperty(
                    PropertyBuilder.New()
                        .SetAccessModifier(AccessModifier.Public)
                        .SetName(field.Name)
                        .SetType(field.Type));

                constructorBuilder.AddParameter(
                    ParameterBuilder.New()
                        .SetName(field.ParameterName)
                        .SetType(field.Type));
            }

            return CodeFileBuilder.New()
                .SetNamespace(descriptor.Namespace)
                .AddType(classBuilder)
                .BuildAsync(writer);
        }

        private CodeBlockBuilder CreateConstructorBody(
            OutputModelDescriptor descriptor)
        {
            var body = new StringBuilder();

            foreach (OutputFieldDescriptor field in descriptor.Fields)
            {
                body.AppendLine($"{field.Name} = {field.ParameterName};");
            }

            return CodeBlockBuilder.FromStringBuilder(body);
        }
    }
}
