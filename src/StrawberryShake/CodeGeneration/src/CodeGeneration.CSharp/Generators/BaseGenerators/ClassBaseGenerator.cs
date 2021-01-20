using System;
using StrawberryShake.CodeGeneration.CSharp.Builders;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public abstract class ClassBaseGenerator<T> : CSharpBaseGenerator<T> where T : ICodeDescriptor
    {
        protected (ClassBuilder, ConstructorBuilder) CreateClassBuilder()
        {
            var classBuilder = ClassBuilder.New();
            var constructorBuilder = ConstructorBuilder.New();
            classBuilder.AddConstructor(constructorBuilder);
            return (classBuilder, constructorBuilder);
        }

        protected void AddConstructorAssignedField(
            TypeReferenceBuilder type,
            string fieldName,
            ClassBuilder classBuilder,
            ConstructorBuilder constructorBuilder)
        {
            var paramName = fieldName.TrimStart('_');

            classBuilder.AddField(
                FieldBuilder
                    .New()
                    .SetReadOnly()
                    .SetName(fieldName)
                    .SetType(type));

            constructorBuilder.AddParameter(
                ParameterBuilder
                    .New()
                    .SetType(type)
                    .SetName(paramName))
                .AddCode(
                    AssignmentBuilder
                        .New()
                        .SetLefthandSide(fieldName)
                        .SetRighthandSide(paramName)
                        .AssertNonNull());
        }

        protected void AddConstructorAssignedField(
            string typename,
            string fieldName,
            ClassBuilder classBuilder,
            ConstructorBuilder constructorBuilder)
        {
            AddConstructorAssignedField(
                TypeReferenceBuilder.New().SetName(typename),
                fieldName,
                classBuilder,
                constructorBuilder);
        }
    }
}
