using System;
using System.Threading.Tasks;
using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.CSharp.Extensions;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public class ClientGenerator : ClassBaseGenerator<ClientDescriptor>
    {
        protected override Task WriteAsync(CodeWriter writer, ClientDescriptor clientDescriptor)
        {
            AssertNonNull(writer, clientDescriptor);


            ClassBuilder.SetName(clientDescriptor.Name);
            ConstructorBuilder.SetTypeName(clientDescriptor.Name);

            foreach (OperationDescriptor operation in clientDescriptor.Operations)
            {
                AddConstructorAssignedField(
                    operation.Name,
                    operation.Name.ToFieldName()
                );

                ClassBuilder.AddMethod(
                    MethodBuilder.New()
                        .SetAccessModifier(AccessModifier.Public)
                        .SetReturnType(operation.Name)
                        .SetName(operation.Name)
                        .AddCode($"return {operation.Name.ToFieldName()};")
                );
            }

            return ClassBuilder.BuildAsync(writer);
        }
    }
}
