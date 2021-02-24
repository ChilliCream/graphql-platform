using StrawberryShake.CodeGeneration.CSharp.Builders;
using static StrawberryShake.CodeGeneration.Utilities.NameUtils;

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
                    GetFieldName(operation.Name),
                    classBuilder,
                    constructorBuilder);

                classBuilder.AddProperty(
                    PropertyBuilder
                        .New()
                        .SetAccessModifier(AccessModifier.Public)
                        .SetType(operation.Name)
                        .SetName(operation.Name)
                        .AsLambda(GetFieldName(operation.Name)));
            }

            CodeFileBuilder
                .New()
                .SetNamespace(descriptor.RuntimeType.NamespaceWithoutGlobal)
                .AddType(classBuilder)
                .Build(writer);
        }
    }
}
