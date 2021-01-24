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

        protected void AddConstructorAssignedField(
            TypeReferenceBuilder type,
            string fieldName,
            ClassBuilder classBuilder,
            ConstructorBuilder constructorBuilder,
            bool skiplNullCheck = false)
        {
            var paramName = fieldName.TrimStart('_');

            classBuilder.AddField(
                FieldBuilder
                    .New()
                    .SetReadOnly()
                    .SetName(fieldName)
                    .SetType(type));

            var assignment = AssignmentBuilder
                .New()
                .SetLefthandSide(fieldName)
                .SetRighthandSide(paramName);
            if (!skiplNullCheck)
            {
                assignment.AssertNonNull();
            }

            constructorBuilder.AddParameter(
                    ParameterBuilder
                        .New()
                        .SetType(type)
                        .SetName(paramName))
                .AddCode(assignment);
        }

        protected void AddConstructorAssignedField(
            string typename,
            string fieldName,
            ClassBuilder classBuilder,
            ConstructorBuilder constructorBuilder,
            bool skiplNullCheck = false)
        {
            AddConstructorAssignedField(
                TypeReferenceBuilder.New().SetName(typename),
                fieldName,
                classBuilder,
                constructorBuilder,
                skiplNullCheck);
        }
    }
}
