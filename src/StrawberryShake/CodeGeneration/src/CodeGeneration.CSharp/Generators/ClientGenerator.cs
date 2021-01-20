using System.Threading.Tasks;
using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.Extensions;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public class ClientGenerator : ClassBaseGenerator<ClientDescriptor>
    {
        protected override Task WriteAsync(CodeWriter writer, ClientDescriptor clientDescriptor)
        {
            AssertNonNull(writer, clientDescriptor);

            var (classBuilder, constructorBuilder) = CreateClassBuilder();

            classBuilder.SetName(clientDescriptor.Name);
            constructorBuilder.SetTypeName(clientDescriptor.Name);

            foreach (OperationDescriptor operation in clientDescriptor.Operations)
            {
                AddConstructorAssignedField(
                    operation.Name,
                    operation.Name.ToFieldName(),
                    classBuilder,
                    constructorBuilder);

                classBuilder.AddMethod(
                    MethodBuilder.New()
                        .SetAccessModifier(AccessModifier.Public)
                        .SetReturnType(operation.Name)
                        .SetName(operation.Name)
                        .AddCode($"return {operation.Name.ToFieldName()};"));
            }

            return CodeFileBuilder.New()
                .SetNamespace(clientDescriptor.Namespace)
                .AddType(classBuilder)
                .BuildAsync(writer);
        }
    }
}
