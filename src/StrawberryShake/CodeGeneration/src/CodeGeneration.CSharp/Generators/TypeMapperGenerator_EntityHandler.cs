using System.Collections.Generic;
using HotChocolate;
using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.Descriptors.TypeDescriptors;
using static StrawberryShake.CodeGeneration.Descriptors.NamingConventions;
using static StrawberryShake.CodeGeneration.Utilities.NameUtils;

namespace StrawberryShake.CodeGeneration.CSharp.Generators
{
    public partial class TypeMapperGenerator
    {
        private const string _entityId = "entityId";
        private const string _snapshot = "snapshot";

        private void AddEntityHandler(
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
                foreach (ObjectTypeDescriptor implementee in interfaceTypeDescriptor.ImplementedBy)
                {
                    NameString dataMapperName =
                        CreateEntityMapperName(implementee.RuntimeType.Name, implementee.Name);

                    if (processed.Add(dataMapperName))
                    {
                        var dataMapperType =
                            TypeNames.IEntityMapper.WithGeneric(
                                CreateEntityType(
                                        implementee.Name,
                                        implementee.RuntimeType.NamespaceWithoutGlobal)
                                    .ToString(),
                                implementee.RuntimeType.Name);

                        AddConstructorAssignedField(
                            dataMapperType,
                            GetFieldName(dataMapperName),
                            GetParameterName(dataMapperName),
                            classBuilder,
                            constructorBuilder);
                    }

                    method.AddCode(GenerateEntityHandlerIfClause(implementee, isNonNullable));
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

            MethodCallBuilder constructorCall = MethodCallBuilder
                .New()
                .SetReturn()
                .SetWrapArguments()
                .SetMethodName(dataMapperName, "Map");

            MethodCallBuilder argument = MethodCallBuilder
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


            IfBuilder ifCorrectType = IfBuilder
                .New()
                .AddCode(constructorCall)
                .SetCondition(
                    MethodCallBuilder
                        .Inline()
                        .SetMethodName(
                            isNonNullable
                                ? new[]
                                {
                                    _entityId,
                                    "Name",
                                    nameof(string.Equals)
                                }
                                : new[]
                                {
                                    _entityId,
                                    "Value",
                                    "Name",
                                    nameof(string.Equals)
                                })
                        .AddArgument(objectTypeDescriptor.Name.AsStringToken())
                        .AddArgument(TypeNames.OrdinalStringComparison));

            return CodeBlockBuilder
                .New()
                .AddEmptyLine()
                .AddCode(ifCorrectType);
        }
    }
}
