using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.Descriptors.TypeDescriptors;
using StrawberryShake.CodeGeneration.Extensions;
using static StrawberryShake.CodeGeneration.Descriptors.NamingConventions;
using static StrawberryShake.CodeGeneration.Utilities.NameUtils;

namespace StrawberryShake.CodeGeneration.CSharp.Generators;

public partial class TypeMapperGenerator
{
    private static void AddComplexDataHandler(
        CSharpSyntaxGeneratorSettings settings,
        ClassBuilder classBuilder,
        ConstructorBuilder constructorBuilder,
        MethodBuilder method,
        ComplexTypeDescriptor complexTypeDescriptor,
        HashSet<string> processed,
        bool isNonNullable)
    {
        var typeInfo = complexTypeDescriptor.ParentRuntimeType
                                   ?? throw new InvalidOperationException();

        method
            .AddParameter(_dataParameterName)
            .SetType(typeInfo.ToString().MakeNullable(!isNonNullable))
            .SetName(_dataParameterName);

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
        method.AddCode($"{complexTypeDescriptor.RuntimeType.Name}? {returnValue};");
        method.AddEmptyLine();

        GenerateIfForEachImplementedBy(
            method,
            complexTypeDescriptor,
            o => GenerateComplexDataInterfaceIfClause(settings, o, returnValue));

        method.AddCode($"return {returnValue};");

        AddRequiredMapMethods(
            settings,
            complexTypeDescriptor,
            classBuilder,
            constructorBuilder,
            processed);
    }

    private static void GenerateIfForEachImplementedBy(
        MethodBuilder method,
        ComplexTypeDescriptor complexTypeDescriptor,
        Func<ObjectTypeDescriptor, IfBuilder> generator)
    {
        if (complexTypeDescriptor is not InterfaceTypeDescriptor interfaceTypeDescriptor ||
            !interfaceTypeDescriptor.ImplementedBy.Any())
        {
            return;
        }

        var dataTypes =
            interfaceTypeDescriptor.ImplementedBy.Where(x => x.IsData()).ToArray();

        var ifChain = generator(dataTypes.First());

        foreach (var objectTypeDescriptor in dataTypes.Skip(1))
        {
            ifChain.AddIfElse(generator(objectTypeDescriptor).SkipIndents());
        }

        ifChain.AddElse(ExceptionBuilder.New(TypeNames.NotSupportedException));

        method.AddCode(ifChain);
    }

    private static IfBuilder GenerateComplexDataInterfaceIfClause(
        CSharpSyntaxGeneratorSettings settings,
        ObjectTypeDescriptor objectTypeDescriptor,
        string variableName)
    {
        var matchedTypeName = GetParameterName(objectTypeDescriptor.Name);

        // since we want to create the data name we will need to craft the type name
        // by hand by using the GraphQL type name and the state namespace.
        var dataTypeName = new RuntimeTypeInfo(
            CreateDataTypeName(objectTypeDescriptor.Name),
            $"{objectTypeDescriptor.RuntimeType.Namespace}.State");

        var block = CodeBlockBuilder.New();

        var constructorCall = MethodCallBuilder
            .Inline()
            .SetNew()
            .SetMethodName(objectTypeDescriptor.RuntimeType.ToString());

        foreach (var prop in objectTypeDescriptor.Properties)
        {
            if (prop.Type.IsEntity() || prop.Type.IsData())
            {
                constructorCall.AddArgument(
                    BuildMapMethodCall(settings, matchedTypeName, prop));
            }
            else if (prop.Type.IsNonNull())
            {
                if (prop.Type.InnerType() is ILeafTypeDescriptor
                        { RuntimeType: { IsValueType: true, }, })
                {
                    block
                        .AddCode(IfBuilder
                            .New()
                            .SetCondition($"!{matchedTypeName}.{prop.Name}.HasValue")
                            .AddCode(ExceptionBuilder.New(TypeNames.ArgumentNullException)))
                        .AddEmptyLine();

                    constructorCall.AddArgument($"{matchedTypeName}.{prop.Name}!.Value");
                }
                else
                {
                    constructorCall
                        .AddArgument(
                            NullCheckBuilder
                                .Inline()
                                .SetCondition($"{matchedTypeName}.{prop.Name}")
                                .SetCode(CodeInlineBuilder.From("default")));
                }
            }
            else
            {
                constructorCall.AddArgument($"{matchedTypeName}.{prop.Name}");
            }
        }

        block.AddCode(AssignmentBuilder
            .New()
            .SetLeftHandSide(variableName)
            .SetRightHandSide(constructorCall));

        return IfBuilder
            .New()
            .SetCondition($"{_dataParameterName} is {dataTypeName} {matchedTypeName}")
            .AddCode(block);
    }
}
