using System;
using System.Threading.Tasks;
using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.CSharp.Extensions;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public class ResultInfoGenerator : ClassBaseGenerator<TypeDescriptor>
    {
        private const string EntityIdsPropertyName = "EntityIds";

        protected override Task WriteAsync(CodeWriter writer, TypeDescriptor typeDescriptor)
        {
            AssertNonNull(writer, typeDescriptor);

            var entityIdsTypeReference = TypeReferenceBuilder.New()
                .SetListType(ListType.List)
                .SetName(WellKnownNames.EntityId);

            ClassBuilder
                .AddImplements(NamingConventions.ResultInfoNameFromTypeName(WellKnownNames.IOperationResultDataInfo))
                .AddProperty(
                    PropertyBuilder.New()
                        .SetName(EntityIdsPropertyName)
                        .SetType(entityIdsTypeReference)
                )
                .SetName(typeDescriptor.Name);

            var entityIdsParamName = EntityIdsPropertyName.WithLowerFirstChar();
            ConstructorBuilder
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
                ClassBuilder.AddProperty(propBuilder);

                // Add initialization of property to the constructor
                var paramName = prop.Name.WithLowerFirstChar();
                ParameterBuilder parameterBuilder = ParameterBuilder.New()
                    .SetName(paramName)
                    .SetType(propTypeBuilder);
                ConstructorBuilder.AddParameter(parameterBuilder);
                ConstructorBuilder.AddCode(prop.Name + " = " + paramName + ";");
            }

            return CodeFileBuilder.New()
                .SetNamespace(typeDescriptor.Namespace)
                .AddType(ClassBuilder)
                .BuildAsync(writer);
        }
    }
}
