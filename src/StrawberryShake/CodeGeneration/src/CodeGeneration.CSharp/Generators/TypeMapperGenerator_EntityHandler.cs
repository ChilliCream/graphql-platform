using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.Descriptors.TypeDescriptors;
using StrawberryShake.CodeGeneration.Extensions;
using static StrawberryShake.CodeGeneration.Descriptors.NamingConventions;
using static StrawberryShake.CodeGeneration.Utilities.NameUtils;

namespace StrawberryShake.CodeGeneration.CSharp.Generators;

public partial class TypeMapperGenerator
{
    private const string EntityId = "entityId";
    private const string Snapshot = "snapshot";

    private static void AddEntityHandler(
        ClassBuilder classBuilder,
        ConstructorBuilder constructorBuilder,
        MethodBuilder method,
        ComplexTypeDescriptor complexTypeDescriptor,
        HashSet<string> processed,
        bool isNonNullable)
    {
        method
            .AddParameter(EntityId)
            .SetType(TypeNames.EntityId.MakeNullable(!isNonNullable));

        method
            .AddParameter(Snapshot)
            .SetType(TypeNames.IEntityStoreSnapshot);

        if (!isNonNullable)
        {
            method.AddCode(EnsureProperNullability(EntityId, isNonNullable));
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
            .SetMethodName(Snapshot, "GetEntity")
            .AddGeneric(CreateEntityType(
                    objectTypeDescriptor.Name,
                    objectTypeDescriptor.RuntimeType.NamespaceWithoutGlobal)
                .ToString())
            .AddArgument(isNonNullable ? EntityId : $"{EntityId}.Value");

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
                                EntityId,
                                "Name",
                                nameof(string.Equals),
                            ]
                            :
                            [
                                EntityId,
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
