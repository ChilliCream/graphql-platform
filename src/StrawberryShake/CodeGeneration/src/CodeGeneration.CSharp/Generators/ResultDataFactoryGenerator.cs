using System;
using System.Threading.Tasks;
using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.CSharp.Extensions;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public class ResultDataFactoryGenerator : CodeGenerator<TypeDescriptor>
    {
        const string StoreParamName = "entityStore";
        private readonly string StoreFieldName = StoreParamName.ToFieldName();

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

            var entityStoretypeReference = TypeReferenceBuilder.New().SetName(WellKnownNames.EntityStore);

            ClassBuilder classBuilder = ClassBuilder.New()
                .AddImplements($"{WellKnownNames.OperationResultDataFactory}<{typeDescriptor.Name}>")
                .AddField(
                    FieldBuilder.New()
                        .SetName(StoreFieldName)
                        .SetType(entityStoretypeReference)
                )
                .SetName(typeDescriptor.Name);

            ConstructorBuilder constructorBuilder = ConstructorBuilder.New()
                .SetTypeName(typeDescriptor.Name)
                .AddParameter(
                    ParameterBuilder.New()
                        .SetType(entityStoretypeReference)
                        .SetName(StoreParamName)
                )
                .AddCode(
                    AssignmentBuilder.New()
                        .SetLefthandSide(StoreFieldName)
                        .SetRighthandSide(StoreParamName)
                )
                .SetAccessModifier(AccessModifier.Public);


            foreach (var prop in typeDescriptor.Properties)
            {

            }

            classBuilder.AddConstructor(constructorBuilder);

            return CodeFileBuilder.New()
                .SetNamespace(typeDescriptor.Namespace)
                .AddType(classBuilder)
                .BuildAsync(writer);
        }
    }
}
