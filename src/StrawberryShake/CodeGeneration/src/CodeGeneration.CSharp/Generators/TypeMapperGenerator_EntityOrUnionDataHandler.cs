using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.Descriptors.TypeDescriptors;

namespace StrawberryShake.CodeGeneration.CSharp.Generators;

public partial class TypeMapperGenerator
{
    private static void AddEntityOrUnionDataHandler(
        CSharpSyntaxGeneratorSettings settings,
        ClassBuilder classBuilder,
        ConstructorBuilder constructorBuilder,
        MethodBuilder method,
        ComplexTypeDescriptor complexTypeDescriptor,
        HashSet<string> processed,
        bool isNonNullable)
    {
        method
            .AddParameter(DataParameterName)
            .SetType(TypeNames.EntityIdOrData.MakeNullable(!isNonNullable))
            .SetName(DataParameterName);

        method
            .AddParameter(Snapshot)
            .SetType(TypeNames.IEntityStoreSnapshot)
            .SetName(Snapshot);

        if (!isNonNullable)
        {
            method.AddCode(EnsureProperNullability(DataParameterName, isNonNullable));
        }

        var dataHandlerMethodName =
            MapMethodNameFromTypeName(complexTypeDescriptor) + "Entity";

        var complexDataHandler = MethodBuilder
            .New()
            .SetReturnType(
                complexTypeDescriptor.RuntimeType.ToString().MakeNullable(!isNonNullable))
            .SetName(dataHandlerMethodName);

        AddComplexDataHandler(settings,
            classBuilder,
            constructorBuilder,
            complexDataHandler,
            complexTypeDescriptor,
            processed,
            isNonNullable);

        classBuilder.AddMethod(complexDataHandler);

        var entityDataHandlerMethodName =
            MapMethodNameFromTypeName(complexTypeDescriptor) + "Data";

        var entityDataHandler = MethodBuilder
            .New()
            .SetReturnType(
                complexTypeDescriptor.RuntimeType.ToString().MakeNullable(!isNonNullable))
            .SetName(entityDataHandlerMethodName);

        AddEntityHandler(
            classBuilder,
            constructorBuilder,
            entityDataHandler,
            complexTypeDescriptor,
            processed,
            isNonNullable);

        classBuilder.AddMethod(entityDataHandler);

        method.AddEmptyLine();

        var parameterName = isNonNullable ? DataParameterName : $"{DataParameterName}.Value";

        var ifBuilder = IfBuilder
            .New()
            .SetCondition($"{parameterName}.EntityId is {{ }} id")
            .AddCode(MethodCallBuilder
                .New()
                .SetReturn()
                .SetMethodName(entityDataHandlerMethodName)
                .AddArgument("id")
                .AddArgument(Snapshot))
            .AddIfElse(IfBuilder
                .New()
                .SetCondition(
                    $"{parameterName}.Data is {complexTypeDescriptor.ParentRuntimeType!} d")
                .AddCode(MethodCallBuilder
                    .New()
                    .SetReturn()
                    .SetMethodName(dataHandlerMethodName)
                    .AddArgument("d")
                    .AddArgument(Snapshot)))
            .AddElse(ExceptionBuilder.New(TypeNames.ArgumentOutOfRangeException));

        method.AddCode(ifBuilder);

        AddRequiredMapMethods(
            settings,
            complexTypeDescriptor,
            classBuilder,
            constructorBuilder,
            processed);
    }
}
