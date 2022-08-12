using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.Descriptors;

namespace StrawberryShake.CodeGeneration.CSharp.Generators;

public abstract class ClassBaseGenerator<T> : CodeGenerator<T> where T : ICodeDescriptor
{
    private static void AddConstructorAssignedField(
        TypeReferenceBuilder type,
        string fieldName,
        string paramName,
        ClassBuilder classBuilder,
        ConstructorBuilder constructorBuilder,
        bool skipNullCheck = false)
    {
        classBuilder.AddField()
            .SetReadOnly()
            .SetName(fieldName)
            .SetType(type);

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

    protected static void AddConstructorAssignedField(
        string typename,
        string fieldName,
        string paramName,
        ClassBuilder classBuilder,
        ConstructorBuilder constructorBuilder,
        bool skipNullCheck = false)
    {
        AddConstructorAssignedField(
            TypeReferenceBuilder.New().SetName(typename),
            fieldName,
            paramName,
            classBuilder,
            constructorBuilder,
            skipNullCheck);
    }
}
