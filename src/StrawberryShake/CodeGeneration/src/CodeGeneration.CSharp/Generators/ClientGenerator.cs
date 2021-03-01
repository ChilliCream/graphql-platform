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
            fileName = descriptor.Name;

            ClassBuilder classBuilder = ClassBuilder
                .New()
                .SetName(fileName);

            ConstructorBuilder constructorBuilder = classBuilder
                .AddConstructor()
                .SetTypeName(fileName);

            classBuilder
                .AddProperty("ClientName")
                .SetPublic()
                .SetType(TypeNames.String)
                .AsLambda(descriptor.Name.Value.AsStringToken());

            foreach (OperationDescriptor operation in descriptor.Operations)
            {
                AddConstructorAssignedField(
                    operation.Name,
                    GetFieldName(operation.Name),
                    classBuilder,
                    constructorBuilder);

                classBuilder
                    .AddProperty(operation.Name)
                    .SetPublic()
                    .SetType(operation.Name)
                    .AsLambda(GetFieldName(operation.Name));
            }

            CodeFileBuilder
                .New()
                .SetNamespace(descriptor.RuntimeType.NamespaceWithoutGlobal)
                .AddType(classBuilder)
                .Build(writer);
        }
    }
}
