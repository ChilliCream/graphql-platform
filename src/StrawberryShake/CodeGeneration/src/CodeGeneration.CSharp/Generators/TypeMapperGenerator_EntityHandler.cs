using System.Collections.Generic;
using System.Linq;
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

            var ifChain = InterfaceImplementeeIf(namedTypeDescriptor.ImplementedBy[0]);

            foreach (NamedTypeDescriptor interfaceImplementee in
                namedTypeDescriptor.ImplementedBy.Skip(1))
            {
                var singleIf = InterfaceImplementeeIf(interfaceImplementee).SkipIndents();
                ifChain.AddIfElse(singleIf);
            }

            ifChain.AddElse(
                CodeInlineBuilder.New()
                    .SetText($"throw new {TypeNames.NotSupportedException}();"));

            method.AddCode(ifChain);

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
                        $"{interfaceImplementee.GraphQLTypeName}\", {TypeNames.OrdinalStringComparisson})");
                }
                else
                {
                    ifCorrectType.SetCondition(
                        $"{EntityIdParamName}?.Name.Equals(\"" +
                        $"{interfaceImplementee.GraphQLTypeName}\", {TypeNames.OrdinalStringComparisson}) ?? false");
                }

                var constructorCall = MethodCallBuilder.New()
                    .SetPrefix($"return {dataMapperName}.")
                    .SetMethodName("Map");

                if (isNonNullable)
                {
                    constructorCall.AddArgument(
                        $"{_storeFieldName}.GetEntity<{EntityTypeNameFromGraphQLTypeName(interfaceImplementee.GraphQLTypeName)}>({EntityIdParamName})");
                }
                else
                {
                    constructorCall.AddArgument(
                        $"{_storeFieldName}.GetEntity<{EntityTypeNameFromGraphQLTypeName(interfaceImplementee.GraphQLTypeName)}>({EntityIdParamName}.Value)");
                }

                method.AddEmptyLine();
                ifCorrectType.AddCode(constructorCall);
                return ifCorrectType;
            }
        }
    }
}
