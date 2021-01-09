using System;
using StrawberryShake.CodeGeneration.CSharp.Builders;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public abstract class ClassBaseGenerator<T>: CSharpBaseGenerator<T> where T : ICodeDescriptor
    {
        protected ClassBuilder ClassBuilder { get; } = ClassBuilder.New();
        protected ConstructorBuilder ConstructorBuilder { get; } = ConstructorBuilder.New();

        protected ClassBaseGenerator()
        {
            ClassBuilder.AddConstructor(ConstructorBuilder);
        }

        protected void AddConstructorAssignedField(TypeReferenceBuilder type, string fieldName)
        {
            var paramName = fieldName.TrimStart('_');

            ClassBuilder.AddField(
                FieldBuilder
                    .New()
                    .SetReadOnly()
                    .SetName(fieldName)
                    .SetType(type));

            ConstructorBuilder.AddParameter(
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

        protected void AddConstructorAssignedField(string typename, string fieldName)
        {
            AddConstructorAssignedField(
                TypeReferenceBuilder.New().SetName(typename),
                fieldName
            );
        }
    }
}
