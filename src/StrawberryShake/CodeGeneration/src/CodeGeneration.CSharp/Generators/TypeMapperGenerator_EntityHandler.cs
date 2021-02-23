using System.Collections.Generic;
using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.Extensions;
using static StrawberryShake.CodeGeneration.NamingConventions;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public partial class TypeMapperGenerator
    {
        private const string EntityIdParamName = "entityId";

        private void AddEntityHandler(
            ClassBuilder classBuilder,
            ConstructorBuilder constructorBuilder,
            MethodBuilder method,
            ComplexTypeDescriptor complexTypeDescriptor,
            HashSet<string> processed,
            bool isNonNullable)
        {
            var nullabilityAdditive = "?";

            if (isNonNullable)
            {
                nullabilityAdditive = "";
            }

            method.AddParameter(
                ParameterBuilder.New()
                    .SetType(TypeNames.EntityId + nullabilityAdditive)
                    .SetName(EntityIdParamName));

            if (!isNonNullable)
            {
                method.AddCode(
                    EnsureProperNullability(
                        EntityIdParamName,
                        isNonNullable));
            }

            if (complexTypeDescriptor is InterfaceTypeDescriptor interfaceTypeDescriptor)
            {
                foreach (ObjectTypeDescriptor implementee in interfaceTypeDescriptor.ImplementedBy)
                {
                    var dataMapperName =
                        EntityMapperNameFromGraphQLTypeName(
                            implementee.RuntimeType.Name,
                            implementee.Name);

                    if (processed.Add(dataMapperName))
                    {
                        var dataMapperType =
                            $"{TypeNames.IEntityMapper}<" +
                            $"{CreateEntityTypeName(implementee.Name)}, " +
                            $"{implementee.Name}>";

                        AddConstructorAssignedField(
                            dataMapperType,
                            dataMapperName.ToFieldName(),
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
                EntityMapperNameFromGraphQLTypeName(
                    objectTypeDescriptor.RuntimeType.Name,
                    objectTypeDescriptor.Name)
                    .ToFieldName();

            var ifCorrectType = IfBuilder.New();

            if (isNonNullable)
            {
                ifCorrectType.SetCondition(
                    $"{EntityIdParamName}.Name.Equals(\"" +
                    $"{objectTypeDescriptor.Name}\", " +
                    $"{TypeNames.OrdinalStringComparisson})");
            }
            else
            {
                ifCorrectType.SetCondition(
                    $"{EntityIdParamName}.Value.Name.Equals(\"" +
                    $"{objectTypeDescriptor.Name}\", " +
                    $"{TypeNames.OrdinalStringComparisson})");
            }

            MethodCallBuilder constructorCall = MethodCallBuilder.New()
                .SetPrefix($"return {dataMapperName}.")
                .SetWrapArguments()
                .SetMethodName(nameof(IEntityMapper<object, object>.Map));

            MethodCallBuilder argument = MethodCallBuilder.New()
                .SetMethodName($"{StoreFieldName}.{nameof(IEntityStore.GetEntity)}")
                .SetDetermineStatement(false)
                .AddGeneric(CreateEntityTypeName(objectTypeDescriptor.Name))
                .AddArgument(isNonNullable ? EntityIdParamName : $"{EntityIdParamName}.Value");

            constructorCall.AddArgument(
                NullCheckBuilder.New()
                    .SetDetermineStatement(false)
                    .SetCondition(argument)
                    .SetCode(ExceptionBuilder
                        .New(TypeNames.GraphQLClientException)
                        .SetDetermineStatement(false)));

            return CodeBlockBuilder.New()
                .AddEmptyLine()
                .AddCode(ifCorrectType);
        }
    }
}
