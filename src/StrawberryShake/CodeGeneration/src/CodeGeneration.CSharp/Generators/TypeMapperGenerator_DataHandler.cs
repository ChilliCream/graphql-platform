using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.Descriptors.TypeDescriptors;
using StrawberryShake.CodeGeneration.Extensions;

namespace StrawberryShake.CodeGeneration.CSharp.Generators;

public partial class TypeMapperGenerator
{
    private const string DataParameterName = "data";

    private static void AddDataHandler(
        CSharpSyntaxGeneratorSettings settings,
        ClassBuilder classBuilder,
        ConstructorBuilder constructorBuilder,
        MethodBuilder method,
        ComplexTypeDescriptor namedTypeDescriptor,
        HashSet<string> processed,
        bool isNonNullable)
    {
        method
            .AddParameter(DataParameterName)
            .SetType(namedTypeDescriptor.ParentRuntimeType!
                .ToString()
                .MakeNullable(!isNonNullable));

        if (settings.IsStoreEnabled())
        {
            method
                .AddParameter(Snapshot)
                .SetType(TypeNames.IEntityStoreSnapshot);
        }

        if (!isNonNullable)
        {
            method.AddCode(EnsureProperNullability(DataParameterName, isNonNullable));
        }

        const string returnValue = nameof(returnValue);

        method.AddCode($"{namedTypeDescriptor.RuntimeType.Name} {returnValue} = default!;");
        method.AddEmptyLine();

        GenerateIfForEachImplementedBy(
            method,
            namedTypeDescriptor,
            o => GenerateDataInterfaceIfClause(settings, o, returnValue));

        method.AddCode($"return {returnValue};");

        AddRequiredMapMethods(
            settings,
            namedTypeDescriptor,
            classBuilder,
            constructorBuilder,
            processed);
    }

    private static IfBuilder GenerateDataInterfaceIfClause(
        CSharpSyntaxGeneratorSettings settings,
        ObjectTypeDescriptor objectTypeDescriptor,
        string variableName)
    {
        ICode ifCondition = MethodCallBuilder
            .Inline()
            .SetMethodName(
                DataParameterName,
                WellKnownNames.TypeName,
                nameof(string.Equals))
            .AddArgument(objectTypeDescriptor.Name.AsStringToken())
            .AddArgument(TypeNames.OrdinalStringComparison);

        var constructorCall = MethodCallBuilder
            .Inline()
            .SetNew()
            .SetMethodName(objectTypeDescriptor.RuntimeType.Name);

        foreach (var prop in objectTypeDescriptor.Properties)
        {
            var propAccess = $"{DataParameterName}.{prop.Name}";
            if (prop.Type.IsEntity() || prop.Type.IsData())
            {
                constructorCall.AddArgument(
                    BuildMapMethodCall(settings, DataParameterName, prop, true));
            }
            else if (prop.Type.IsNullable())
            {
                constructorCall.AddArgument(propAccess);
            }
            else
            {
                constructorCall
                    .AddArgument(
                        NullCheckBuilder
                            .Inline()
                            .SetCondition(propAccess)
                            .SetCode(ExceptionBuilder.Inline(TypeNames.ArgumentNullException)));
            }
        }

        return IfBuilder
            .New()
            .SetCondition(ifCondition)
            .AddCode(AssignmentBuilder
                .New()
                .SetLeftHandSide(variableName)
                .SetRightHandSide(constructorCall));
    }
}
