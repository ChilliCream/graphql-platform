using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.Descriptors.TypeDescriptors;
using StrawberryShake.CodeGeneration.Extensions;
using static StrawberryShake.CodeGeneration.Descriptors.NamingConventions;
using static StrawberryShake.CodeGeneration.Utilities.NameUtils;

namespace StrawberryShake.CodeGeneration.CSharp.Generators;

public partial class TypeMapperGenerator
{
    private const string _entityId = "entityId";
    private const string _snapshot = "snapshot";

    private static void AddEntityHandler(
        ClassBuilder classBuilder,
        ConstructorBuilder constructorBuilder,
        MethodBuilder method,
        ComplexTypeDescriptor complexTypeDescriptor,
        HashSet<string> processed,
        bool isNonNullable)
    {
        method
            .AddParameter(_entityId)
            .SetType(TypeNames.EntityId.MakeNullable(!isNonNullable));

        method
            .AddParameter(_snapshot)
            .SetType(TypeNames.IEntityStoreSnapshot);

        if (!isNonNullable)
        {
            method.AddCode(EnsureProperNullability(_entityId, isNonNullable));
        }

        if (complexTypeDescriptor is InterfaceTypeDescriptor interfaceTypeDescriptor)
        {
            foreach (var implementer in
                     interfaceTypeDescriptor.ImplementedBy.Where(x => x.IsEntity()))
            {
                var dataMapperName =
                    CreateEntityMapperName(
                        implementer.RuntimeType.Name,
                        implementer.Name);

                if (processed.Add(dataMapperName))
                {
                    var dataMapperType =
                        TypeNames.IEntityMapper.WithGeneric(
                            CreateEntityType(
                                implementer.Name,
                                implementer.RuntimeType.NamespaceWithoutGlobal).ToString(),
                            implementer.RuntimeType.Name);

                    AddConstructorAssignedField(
                        dataMapperType,
                        GetFieldName(dataMapperName),
                        GetParameterName(dataMapperName),
                        classBuilder,
                        constructorBuilder);
                }

                method.AddCode(GenerateEntityHandlerIfClause(implementer, isNonNullable));
            }
        }

        method.AddCode(ExceptionBuilder.New(TypeNames.NotSupportedException));
    }

    private static ICode GenerateEntityHandlerIfClause(
        ObjectTypeDescriptor objectTypeDescriptor,
        bool isNonNullable)
    {
        var dataMapperName =
            GetFieldName(
                CreateEntityMapperName(
                    objectTypeDescriptor.RuntimeType.Name,
                    objectTypeDescriptor.Name));

        var constructorCall = MethodCallBuilder
            .New()
            .SetReturn()
            .SetWrapArguments()
            .SetMethodName(dataMapperName, "Map");

        var argument = MethodCallBuilder
            .Inline()
            .SetMethodName(_snapshot, "GetEntity")
            .AddGeneric(CreateEntityType(
                    objectTypeDescriptor.Name,
                    objectTypeDescriptor.RuntimeType.NamespaceWithoutGlobal)
                .ToString())
            .AddArgument(isNonNullable ? _entityId : $"{_entityId}.Value");

        constructorCall.AddArgument(
            NullCheckBuilder
                .New()
                .SetDetermineStatement(false)
                .SetCondition(argument)
                .SetCode(ExceptionBuilder.Inline(TypeNames.GraphQLClientException)));

        var ifCorrectType = IfBuilder
            .New()
            .AddCode(constructorCall)
            .SetCondition(
                MethodCallBuilder
                    .Inline()
                    .SetMethodName(
                        isNonNullable
                            ?
                            [
                                _entityId,
                                "Name",
                                nameof(string.Equals),
                            ]
                            :
                            [
                                _entityId,
                                "Value",
                                "Name",
                                nameof(string.Equals),
                            ])
                    .AddArgument(objectTypeDescriptor.Name.AsStringToken())
                    .AddArgument(TypeNames.OrdinalStringComparison));

        return CodeBlockBuilder
            .New()
            .AddEmptyLine()
            .AddCode(ifCorrectType);
    }
}
