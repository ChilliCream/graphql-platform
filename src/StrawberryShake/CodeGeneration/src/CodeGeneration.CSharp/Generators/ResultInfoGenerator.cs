using System;
using System.Threading.Tasks;
using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.CSharp.Extensions;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public class ResultInfoGenerator : CodeGenerator<TypeDescriptor>
    {
        private const string EntityIdsPropertyName = "EntityIds";

        protected override Task WriteAsync(CodeWriter writer, TypeDescriptor typeDescriptor)
        {
            if (writer is null)
            {
                throw new ArgumentNullException(nameof(writer));
            }

            if (typeDescriptor is null)
            {
                throw new ArgumentNullException(nameof(typeDescriptor));
            }

            var entityIdsTypeReference = TypeReferenceBuilder.New()
                .SetListType(ListType.List)
                .SetName(WellKnownNames.EntityId);

            ClassBuilder classBuilder = ClassBuilder.New()
                .AddImplements(NamingConventions.ResultIntoNameFromTypeName(WellKnownNames.IOperationResultDataInfo))
                .AddProperty(
                    PropertyBuilder.New()
                        .SetName(EntityIdsPropertyName)
                        .SetType(entityIdsTypeReference)
                )
                .SetName(typeDescriptor.Name);

            var entityIdsParamName = EntityIdsPropertyName.WithLowerFirstChar();
            ConstructorBuilder constructorBuilder = ConstructorBuilder.New()
                .SetTypeName(typeDescriptor.Name)
                .AddParameter(
                    ParameterBuilder.New()
                        .SetType(entityIdsTypeReference)
                        .SetName(entityIdsParamName)
                )
                .AddCode(
                    AssignmentBuilder.New()
                        .SetLefthandSide(EntityIdsPropertyName)
                        .SetRighthandSide(entityIdsParamName)
                )
                .SetAccessModifier(AccessModifier.Public);


            foreach (var prop in typeDescriptor.Properties)
            {
                var propTypeBuilder = prop.TypeReference.ToEntityIdBuilder();
                // Add Property to class
                var propBuilder = PropertyBuilder
                    .New()
                    .SetName(prop.Name)
                    .SetType(propTypeBuilder)
                    .SetAccessModifier(AccessModifier.Public);
                classBuilder.AddProperty(propBuilder);

                // Add initialization of property to the constructor
                var paramName = prop.Name.WithLowerFirstChar();
                ParameterBuilder parameterBuilder = ParameterBuilder.New()
                    .SetName(paramName)
                    .SetType(propTypeBuilder);
                constructorBuilder.AddParameter(parameterBuilder);
                constructorBuilder.AddCode(prop.Name + " = " + paramName + ";");
            }

            classBuilder.AddConstructor(constructorBuilder);

            return CodeFileBuilder.New()
                .SetNamespace(typeDescriptor.Namespace)
                .AddType(classBuilder)
                .BuildAsync(writer);
        }
    }
}
