using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.Extensions;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public class ClientGenerator : ClassBaseGenerator<ClientDescriptor>
    {
        protected override void Generate(
            CodeWriter writer,
            ClientDescriptor descriptor,
            out string fileName)
        {
            var (classBuilder, constructorBuilder) = CreateClassBuilder();

            fileName = descriptor.Name;
            classBuilder.SetName(fileName);
            constructorBuilder.SetTypeName(fileName);

            foreach (OperationDescriptor operation in descriptor.Operations)
            {
                AddConstructorAssignedField(
                    operation.Name,
                    operation.Name.ToFieldName(),
                    classBuilder,
                    constructorBuilder);

                classBuilder.AddProperty(
                    PropertyBuilder
                        .New()
                        .SetAccessModifier(AccessModifier.Public)
                        .SetType(operation.Name)
                        .SetName(operation.Name)
                        .AsLambda(operation.Name.ToFieldName()));
            }

            CodeFileBuilder
                .New()
                .SetNamespace(descriptor.RuntimeType.NamespaceWithoutGlobal)
                .AddType(classBuilder)
                .Build(writer);
        }
    }
}
