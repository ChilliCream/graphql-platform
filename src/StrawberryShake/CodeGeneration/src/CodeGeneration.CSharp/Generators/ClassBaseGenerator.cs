using StrawberryShake.CodeGeneration.CSharp.Builders;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public abstract class ClassBaseGenerator<T> : CodeGenerator<T> where T : ICodeDescriptor
    {
        protected ClassBuilder ClassBuilder = ClassBuilder.New();
        protected ConstructorBuilder ConstructorBuilder = ConstructorBuilder.New();

        protected ClassBaseGenerator()
        {
            ClassBuilder.AddConstructor(ConstructorBuilder);
        }

        protected void ConstructorAssignedField(TypeReferenceBuilder type, string fieldName)
        {
            var paramName = fieldName.TrimStart('_');

            ClassBuilder.AddField(
                FieldBuilder.New()
                    .SetReadOnly()
                    .SetName(fieldName)
                    .SetType(type)
            );

            ConstructorBuilder.AddParameter(
                    ParameterBuilder.New()
                        .SetType(type)
                        .SetName(paramName)
                )
                .AddCode(
                    AssignmentBuilder.New()
                        .SetLefthandSide(fieldName)
                        .SetRighthandSide(paramName)
                        .AssertNonNull()
                );
        }

        protected void ConstructorAssignedField(string typename, string paramName)
        {
            ConstructorAssignedField(
                TypeReferenceBuilder.New().SetName(typename),
                paramName
            );
        }
    }
}
