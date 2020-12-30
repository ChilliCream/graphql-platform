using System;
using System.Threading.Tasks;
using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.CSharp.Extensions;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public class ResultDataFactoryGenerator : ClassBaseGenerator<TypeDescriptor>
    {
        const string StoreParamName = "_entityStore";

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

            ClassBuilder
                .SetName(NamingConventions.ResultFactoryNameFromTypeName(typeDescriptor.Name))
                .AddImplements($"{WellKnownNames.IOperationResultDataFactory}<{typeDescriptor.Name}>");

            ConstructorBuilder
                .SetTypeName(typeDescriptor.Name)
                .SetAccessModifier(AccessModifier.Public);

            ConstructorAssignedField(
                WellKnownNames.IEntityStore,
                StoreParamName
            );


            var mappersToInject = typeDescriptor.IsInterface
                ? typeDescriptor.IsImplementedBy
                : new[] {typeDescriptor.Name};

            foreach (var mapperType in mappersToInject)
            {
                var typeName = TypeReferenceBuilder
                    .New()
                    .SetName(WellKnownNames.IEntityMapper)
                    .AddGeneric(NamingConventions.EntityTypeNameFromTypeName(mapperType))
                    .AddGeneric(mapperType);

                ConstructorAssignedField(
                    typeName,
                    NamingConventions.MapperNameFromTypeName(mapperType).ToFieldName()
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
