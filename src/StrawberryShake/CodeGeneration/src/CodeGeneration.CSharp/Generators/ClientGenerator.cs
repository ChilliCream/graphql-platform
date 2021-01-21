using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.Extensions;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public class ClientGenerator : ClassBaseGenerator<ClientDescriptor>
    {
        protected override void Generate(CodeWriter writer, ClientDescriptor descriptor)
        {
            var (classBuilder, constructorBuilder) = CreateClassBuilder();

            classBuilder.SetName(descriptor.Name);
            constructorBuilder.SetTypeName(descriptor.Name);

            foreach (OperationDescriptor operation in descriptor.Operations)
            {
                AddConstructorAssignedField(
                    operation.Name,
                    operation.Name.ToFieldName(),
                    classBuilder,
                    constructorBuilder);

                classBuilder.AddMethod(
                    MethodBuilder
                        .New()
                        .SetAccessModifier(AccessModifier.Public)
                        .SetReturnType(operation.Name)
                        .SetName(operation.Name)
                        .AddCode($"return {operation.Name.ToFieldName()};"));
            }

            CodeFileBuilder
                .New()
                .SetNamespace(descriptor.Namespace)
                .AddType(classBuilder)
                .Build(writer);
        }
    }
}
