using System.Collections.Generic;
using HotChocolate.Types;
using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.CSharp.Extensions;
using StrawberryShake.CodeGeneration.Extensions;
using static StrawberryShake.CodeGeneration.NamingConventions;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public partial class JsonResultBuilderGenerator
    {
        private void AddUpdateEntityMethod(
            ClassBuilder classBuilder,
            MethodBuilder methodBuilder,
            NamedTypeDescriptor namedTypeDescriptor)
        {
            var entityIdVarName = "entityId";
            methodBuilder.AddCode(
                AssignmentBuilder.New()
                    .SetLefthandSide(
                        CodeBlockBuilder.New()
                            .AddCode(TypeNames.EntityId)
                            .AddCode($" {entityIdVarName}"))
                    .SetRighthandSide($"{_extractIdFieldName}({_objParamName}.Value)"));

            methodBuilder.AddCode($"{_entityIdsParam}.Add({entityIdVarName});");
            methodBuilder.AddEmptyLine();

            var entityVarName = "entity";

            if (namedTypeDescriptor.IsInterface)
            {
                // If the type is an interface
                foreach (NamedTypeDescriptor concreteType in namedTypeDescriptor.ImplementedBy)
                {
                    methodBuilder.AddEmptyLine();
                    var ifStatement = IfBuilder.New()
                        .SetCondition(
                            $"entityId.Name.Equals(\"{concreteType.Name}\", " +
                            $"{TypeNames.OrdinalStringComparisson})");

                    var entityTypeName =
                        EntityTypeNameFromGraphQLTypeName(concreteType.GraphQLTypeName);

                    WriteEntityLoader(
                        ifStatement,
                        entityTypeName,
                        entityVarName,
                        entityIdVarName);

                    WritePropertyAssignments(
                        ifStatement,
                        concreteType.Properties,
                        entityVarName);

                    ifStatement.AddEmptyLine();
                    ifStatement.AddCode($"return {entityIdVarName};");
                    methodBuilder.AddCode(ifStatement);
                }

                methodBuilder.AddEmptyLine();
                methodBuilder.AddCode($"throw new {TypeNames.NotSupportedException}();");
            }
            else
            {
                WriteEntityLoader(
                    methodBuilder,
                    EntityTypeNameFromGraphQLTypeName(namedTypeDescriptor.GraphQLTypeName),
                    entityVarName,
                    entityIdVarName);

                WritePropertyAssignments(
                    methodBuilder,
                    namedTypeDescriptor.Properties,
                    entityVarName);

                methodBuilder.AddEmptyLine();
                methodBuilder.AddCode($"return {entityIdVarName};");
            }

            AddRequiredDeserializeMethods(
                namedTypeDescriptor,
                classBuilder);
        }

        private void WriteEntityLoader<T>(
            ICodeContainer<T> codeContainer,
            string entityTypeName,
            string entityVarName,
            string entityIdVarName)
        {
            codeContainer.AddCode(
                $"{entityTypeName} {entityVarName} = {_entityStoreFieldName}" +
                $".GetOrCreate<{entityTypeName}>({entityIdVarName});");
        }

        private void WritePropertyAssignments<T>(ICodeContainer<T> codeContainer,
            IReadOnlyList<PropertyDescriptor> properties, string entityVarName)
        {
            foreach (PropertyDescriptor property in properties)
            {
                codeContainer.AddCode(
                    AssignmentBuilder.New()
                        .SetLefthandSide($"{entityVarName}.{property.Name}")
                        .SetRighthandSide(BuildUpdateMethodCall(property)));
            }
        }
    }
}
