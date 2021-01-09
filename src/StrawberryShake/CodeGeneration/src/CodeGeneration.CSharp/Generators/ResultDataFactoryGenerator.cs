using System;
using System.Threading.Tasks;
using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.CSharp.Extensions;
using StrawberryShake.CodeGeneration.Extensions;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public class ResultDataFactoryGenerator : ClassBaseGenerator<TypeDescriptor>
    {
        const string StoreParamName = "_entityStore";

        protected override Task WriteAsync(CodeWriter writer, TypeDescriptor typeDescriptor)
        {
            AssertNonNull(
                writer,
                typeDescriptor
            );

            ClassBuilder
                .SetName(NamingConventions.ResultFactoryNameFromTypeName(typeDescriptor.Name))
                .AddImplements($"{WellKnownNames.IOperationResultDataFactory}<{typeDescriptor.Name}>");

            ConstructorBuilder
                .SetTypeName(typeDescriptor.Name)
                .SetAccessModifier(AccessModifier.Public);

            AddConstructorAssignedField(
                WellKnownNames.IEntityStore,
                StoreParamName
            );


            var mappersToInject = typeDescriptor.IsInterface
                ? typeDescriptor.IsImplementedBy
                : new[] {typeDescriptor};

            foreach (var mapperType in mappersToInject)
            {
                var typeName = TypeReferenceBuilder
                    .New()
                    .SetName(WellKnownNames.IEntityMapper)
                    .AddGeneric(NamingConventions.EntityTypeNameFromTypeName(mapperType.Name))
                    .AddGeneric(mapperType.Name);

                AddConstructorAssignedField(
                    typeName,
                    NamingConventions.MapperNameFromTypeName(mapperType.Name).ToFieldName()
                );
            }

            foreach (var prop in typeDescriptor.Properties)
            {
            }

            return CodeFileBuilder.New()
                .SetNamespace(typeDescriptor.Namespace)
                .AddType(ClassBuilder)
                .BuildAsync(writer);
        }
    }
}
