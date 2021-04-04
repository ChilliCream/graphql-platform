using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.Descriptors;
using StrawberryShake.CodeGeneration.Descriptors.Operations;
using static StrawberryShake.CodeGeneration.Utilities.NameUtils;

namespace StrawberryShake.CodeGeneration.CSharp.Generators
{
    public class ClientGenerator : ClassBaseGenerator<ClientDescriptor>
    {
        protected override void Generate(
            CodeWriter writer,
            ClientDescriptor descriptor,
            CodeGeneratorSettings settings,
            out string fileName,
            out string? path)
        {
            fileName = descriptor.Name;
            path = null;

            ClassBuilder classBuilder = ClassBuilder
                .New()
                .SetName(fileName)
                .SetComment(descriptor.Documentation)
                .AddImplements(descriptor.InterfaceType.ToString());

            ConstructorBuilder constructorBuilder = classBuilder
                .AddConstructor()
                .SetTypeName(fileName);

            classBuilder
                .AddProperty("ClientName")
                .SetPublic()
                .SetStatic()
                .SetType(TypeNames.String)
                .AsLambda(descriptor.Name.Value.AsStringToken());

            foreach (OperationDescriptor operation in descriptor.Operations)
            {
                AddConstructorAssignedField(
                    operation.InterfaceType.ToString(),
                    GetFieldName(operation.Name),
                    GetParameterName(operation.Name),
                    classBuilder,
                    constructorBuilder);

                classBuilder
                    .AddProperty(GetPropertyName(operation.Name))
                    .SetPublic()
                    .SetType(operation.InterfaceType.ToString())
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
