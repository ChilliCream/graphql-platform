using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.Descriptors.TypeDescriptors;
using StrawberryShake.CodeGeneration.Extensions;

namespace StrawberryShake.CodeGeneration.CSharp.Generators;

public partial class TypeMapperGenerator
{
    private const string _dataParameterName = "data";

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
            .AddParameter(_dataParameterName)
            .SetType(namedTypeDescriptor.ParentRuntimeType!
                .ToString()
                .MakeNullable(!isNonNullable));

        if (settings.IsStoreEnabled())
        {
            method
                .AddParameter(_snapshot)
                .SetType(TypeNames.IEntityStoreSnapshot);
        }

        if (!isNonNullable)
        {
            method.AddCode(EnsureProperNullability(_dataParameterName, isNonNullable));
        }

        const string returnValue = nameof(returnValue);

        method.AddCode($"{namedTypeDescriptor.RuntimeType.Name} {returnValue} = default!;");
        method.AddEmptyLine();

        GenerateIfForEachImplementedBy(
            method,
            namedTypeDescriptor,
            o => GenerateDataInterfaceIfClause(settings, o, isNonNullable, returnValue));

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
        bool isNonNullable,
        string variableName)
    {
        ICode ifCondition = MethodCallBuilder
            .Inline()
            .SetMethodName(
                _dataParameterName.MakeNullable(!isNonNullable),
                WellKnownNames.TypeName,
                nameof(string.Equals))
            .AddArgument(objectTypeDescriptor.Name.AsStringToken())
            .AddArgument(TypeNames.OrdinalStringComparison);

        if (!isNonNullable)
        {
            ifCondition = NullCheckBuilder
                .New()
                .SetCondition(ifCondition)
                .SetSingleLine()
                .SetDetermineStatement(false)
                .SetCode("false");
        }

        var constructorCall = MethodCallBuilder
            .Inline()
            .SetNew()
            .SetMethodName(objectTypeDescriptor.RuntimeType.Name);

        foreach (var prop in objectTypeDescriptor.Properties)
        {
            var propAccess = $"{_dataParameterName}.{prop.Name}";
            if (prop.Type.IsEntity() || prop.Type.IsData())
            {
                constructorCall.AddArgument(
                    BuildMapMethodCall(settings, _dataParameterName, prop, true));
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
                            .SetCode(CodeInlineBuilder.From("default")));
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
