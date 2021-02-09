using System.Collections.Generic;
using System.Linq;
using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.Extensions;
using static StrawberryShake.CodeGeneration.NamingConventions;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public partial class ResultFromEntityTypeMapperGenerator
    {
        private const string EntityIdParamName = "entityId";

        private void AddEntityHandler(
            ITypeDescriptor rootDescriptor,
            ClassBuilder classBuilder,
            ConstructorBuilder constructorBuilder,
            MethodBuilder method,
            NamedTypeDescriptor namedTypeDescriptor,
            HashSet<string> processed)
        {
            var nullabilityAdditive = "?";
            if (rootDescriptor.IsNonNullableType())
            {
                nullabilityAdditive = "";
            }
            method.AddParameter(
                ParameterBuilder.New()
                    .SetType(TypeNames.EntityId + nullabilityAdditive)
                    .SetName(EntityIdParamName));

            method.AddCode(
                EnsureProperNullability(
                    EntityIdParamName,
                    rootDescriptor.IsNonNullableType()));

            foreach (NamedTypeDescriptor implementee in namedTypeDescriptor.ImplementedBy)
            {
                var dataMapperName =
                    EntityMapperNameFromGraphQLTypeName(
                        implementee.Name,
                        implementee.GraphQLTypeName);

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

                var ifCorrectType = IfBuilder.New()
                    .SetCondition(
                        $"{EntityIdParamName}?.Name.Equals(\"" +
                        $"{interfaceImplementee.GraphQLTypeName}\", {TypeNames.OrdinalStringComparisson}) ?? false");

                var constructorCall = MethodCallBuilder.New()
                    .SetPrefix($"return {dataMapperName}.")
                    .SetMethodName("Map")
                    .AddArgument($"{_storeFieldName}.GetEntity<{EntityTypeNameFromGraphQLTypeName(interfaceImplementee.GraphQLTypeName)}>({EntityIdParamName}.Value)");

                method.AddEmptyLine();
                ifCorrectType.AddCode(constructorCall);
                return ifCorrectType;
            }
        }
    }
}
