using StrawberryShake.CodeGeneration.CSharp.Builders;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public abstract class ClassBaseGenerator<T> : CodeGenerator<T> where T : ICodeDescriptor
    {
        protected (ClassBuilder, ConstructorBuilder) CreateClassBuilder(
            bool addConstructorToClass = true)
        {
            var classBuilder = ClassBuilder.New();
            var constructorBuilder = ConstructorBuilder.New();
            if (addConstructorToClass)
            {
                classBuilder.AddConstructor(constructorBuilder);
            }
            return (classBuilder, constructorBuilder);
        }

        protected void AddConstructorAssignedField(
            TypeReferenceBuilder type,
            string fieldName,
            ClassBuilder classBuilder,
            ConstructorBuilder constructorBuilder,
            bool skipNullCheck = false)
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

            if (!skipNullCheck)
            {
                assignment.AssertNonNull();
            }

            constructorBuilder
                .AddCode(assignment)
                .AddParameter(paramName, b => b.SetType(type));
        }

        protected void AddConstructorAssignedField(
            string typename,
            string fieldName,
            ClassBuilder classBuilder,
            ConstructorBuilder constructorBuilder,
            bool skipNullCheck = false)
        {
            AddConstructorAssignedField(
                TypeReferenceBuilder.New().SetName(typename),
                fieldName,
                classBuilder,
                constructorBuilder,
                skipNullCheck);
        }
    }
}
