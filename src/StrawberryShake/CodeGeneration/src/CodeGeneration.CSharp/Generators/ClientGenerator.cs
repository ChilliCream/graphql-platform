using System.Threading.Tasks;
using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.CSharp.Extensions;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public class ClientGenerator : CodeGenerator<ClientDescriptor>
    {
        protected override Task WriteAsync(CodeWriter writer, ClientDescriptor descriptor)
        {
            var classBuilder = ClassBuilder.New()
                .SetName(descriptor.Name);

            var constructorBuilder = ConstructorBuilder.New()
                .SetTypeName(descriptor.Name)
                .SetAccessModifier(AccessModifier.Public);


            foreach (OperationDescriptor operation in descriptor.Operations)
            {
                classBuilder.AddField(
                    FieldBuilder.New()
                        .SetReadOnly()
                        .SetType(operation.Name)
                        .SetName(operation.Name.ToFieldName())
                );

                constructorBuilder.AddParameter(
                    ParameterBuilder.New()
                        .SetName(operation.Name.WithLowerFirstChar())
                        .SetType(operation.Name)
                );

                constructorBuilder.AddCode(
                    CodeLineBuilder.New()
                        .SetLine($"{operation.Name.ToFieldName()} =  {operation.Name.WithLowerFirstChar()};")
                );

                classBuilder.AddMethod(
                    MethodBuilder.New()
                        .SetAccessModifier(AccessModifier.Public)
                        .SetReturnType(operation.Name)
                        .SetName(operation.Name)
                        .AddCode($"return {operation.Name.ToFieldName()};")
                );
            }

            classBuilder.AddConstructor(constructorBuilder);
            return classBuilder.BuildAsync(writer);
        }
    }
}
