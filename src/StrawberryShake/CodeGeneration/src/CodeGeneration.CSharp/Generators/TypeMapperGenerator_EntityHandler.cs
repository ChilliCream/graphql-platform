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
            NamedTypeDescriptor namedTypeDescriptor,
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


            foreach (NamedTypeDescriptor implementee in namedTypeDescriptor.ImplementedBy)
            {
                var dataMapperName =
                    EntityMapperNameFromGraphQLTypeName(
                        implementee.Name,
                        implementee.GraphQLTypeName);

                if (processed.Add(dataMapperName))
                {
                    var dataMapperType =
                        $"{TypeNames.IEntityMapper}<" +
                        $"{EntityTypeNameFromGraphQLTypeName(implementee.GraphQLTypeName)}, " +
                        $"{implementee.Name}>";

                    AddConstructorAssignedField(
                        dataMapperType,
                        dataMapperName.ToFieldName(),
                        classBuilder,
                        constructorBuilder);
                }
            }

            foreach (NamedTypeDescriptor interfaceImplementee in namedTypeDescriptor.ImplementedBy)
            {
                method.AddCode(InterfaceImplementeeIf(interfaceImplementee));
            }

            method.AddCode(ExceptionBuilder.New(TypeNames.NotSupportedException));

            IfBuilder InterfaceImplementeeIf(NamedTypeDescriptor interfaceImplementee)
            {
                var dataMapperName =
                    EntityMapperNameFromGraphQLTypeName(
                            interfaceImplementee.Name,
                            interfaceImplementee.GraphQLTypeName)
                        .ToFieldName();

                var ifCorrectType = IfBuilder.New();

                if (isNonNullable)
                {
                    ifCorrectType.SetCondition(
                        $"{EntityIdParamName}.Name.Equals(\"" +
                        $"{interfaceImplementee.GraphQLTypeName}\", " +
                        $"{TypeNames.OrdinalStringComparisson})");
                }
                else
                {
                    ifCorrectType.SetCondition(
                        $"{EntityIdParamName}.Value.Name.Equals(\"" +
                        $"{interfaceImplementee.GraphQLTypeName}\", " +
                        $"{TypeNames.OrdinalStringComparisson})");
                }

                MethodCallBuilder constructorCall = MethodCallBuilder.New()
                    .SetPrefix($"return {dataMapperName}.")
                    .SetWrapArguments()
                    .SetMethodName(nameof(IEntityMapper<object, object>.Map));

                MethodCallBuilder argument = MethodCallBuilder.New()
                    .SetMethodName($"{StoreFieldName}.{nameof(IEntityStore.GetEntity)}")
                    .SetDetermineStatement(false)
                    .AddGeneric(
                        EntityTypeNameFromGraphQLTypeName(interfaceImplementee.GraphQLTypeName))
                    .AddArgument(isNonNullable ? EntityIdParamName : $"{EntityIdParamName}.Value");

                constructorCall.AddArgument(
                    NullCheckBuilder.New()
                        .SetDetermineStatement(false)
                        .SetCondition(argument)
                        .SetCode(ExceptionBuilder
                            .New(TypeNames.GraphQLClientException)
                            .SetDetermineStatement(false)));

                method.AddEmptyLine();
                ifCorrectType.AddCode(constructorCall);
                return ifCorrectType;
            }
        }
    }
}
