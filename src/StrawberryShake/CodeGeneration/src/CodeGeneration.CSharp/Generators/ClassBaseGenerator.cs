using StrawberryShake.CodeGeneration.CSharp.Builders;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public abstract class ClassBaseGenerator<T> : CodeGenerator<T> where T : ICodeDescriptor
    {
        protected (ClassBuilder, ConstructorBuilder) CreateClassBuilder()
        {
            var classBuilder = ClassBuilder.New();
            var constructorBuilder = ConstructorBuilder.New();
            classBuilder.AddConstructor(constructorBuilder);
            return (classBuilder, constructorBuilder);
        }

        protected void AddConstructorAssignedNonNullableField(
            ITypeReferenceBuilder type,
            string fieldName,
            ClassBuilder classBuilder,
            ConstructorBuilder constructorBuilder)
        {
            var nonNullableType = NonNullTypeReferenceBuilder.New().SetInnerType(type);
            var paramName = fieldName.TrimStart('_');

            classBuilder.AddField(
                FieldBuilder
                    .New()
                    .SetReadOnly()
                    .SetName(fieldName)
                    .SetType(nonNullableType));

            constructorBuilder.AddParameter(
                ParameterBuilder
                    .New()
                    .SetType(nonNullableType)
                    .SetName(paramName))
                .AddCode(
                    AssignmentBuilder
                        .New()
                        .SetLefthandSide(fieldName)
                        .SetRighthandSide(paramName)
                        .AssertNonNull());
        }

        protected void AddConstructorAssignedNonNullableField(
            string typename,
            string fieldName,
            ClassBuilder classBuilder,
            ConstructorBuilder constructorBuilder)
        {
            AddConstructorAssignedNonNullableField(
                TypeReferenceBuilder.New().SetName(typename),
                fieldName,
                classBuilder,
                constructorBuilder);
        }
    }
}
