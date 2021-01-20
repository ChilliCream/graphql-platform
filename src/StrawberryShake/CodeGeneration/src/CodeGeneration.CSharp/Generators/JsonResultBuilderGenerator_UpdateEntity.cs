using HotChocolate.Types;
using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.Extensions;
using static StrawberryShake.CodeGeneration.NamingConventions;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public partial class JsonResultBuilderGenerator
    {
        private void AddUpdateEntityMethod(NamedTypeDescriptor namedTypeDescriptor, ITypeDescriptor originalTypeDescriptor)
        {
            var updateEntityMethod = MethodBuilder.New()
                .SetAccessModifier(AccessModifier.Private)
                .SetName(DeserializerMethodNameFromTypeName(originalTypeDescriptor))
                .SetReturnType(WellKnownNames.EntityId)
                .AddParameter(
                    ParameterBuilder.New()
                        .SetType(_jsonElementParamName)
                        .SetName(_objParamName))
                .AddParameter(
                    ParameterBuilder.New()
                        .SetType($"ISet<{WellKnownNames.EntityId}>")
                        .SetName(_entityIdsParam));

            updateEntityMethod.AddCode(
                EnsureJsonValueIsNotNull(),
                originalTypeDescriptor.IsNonNullableType());

            var entityIdVarName = "entityId";
            updateEntityMethod.AddCode(
                $"{WellKnownNames.EntityId} {entityIdVarName} = " +
                $"{_extractIdFieldName}({_objParamName});");
            updateEntityMethod.AddCode($"{_entityIdsParam}.Add({entityIdVarName});");

            // If the type is an interface
            foreach (NamedTypeDescriptor concreteType in namedTypeDescriptor.ImplementedBy)
            {
                updateEntityMethod.AddEmptyLine();
                var ifStatement = IfBuilder.New()
                    .SetCondition(
                        $"entityId.Name.Equals(\"{concreteType.Name}\", " +
                        "StringComparison.Ordinal)");

                var entityTypeName = EntityTypeNameFromGraphQLTypeName(concreteType.Name);

                var entityVarName = "entity";
                ifStatement.AddCode(
                    $"{entityTypeName} {entityTypeName} = {_entityStoreFieldName}" +
                    $".GetOrCreate<{entityTypeName}>({entityIdVarName});");

                foreach (PropertyDescriptor property in concreteType.Properties)
                {
                    ifStatement.AddCode(
                        AssignmentBuilder.New()
                            .SetLefthandSide($"{entityVarName}.{property.Name}")
                            .SetRighthandSide(BuildUpdateMethodCall(property)));
                }

                ifStatement.AddEmptyLine();
                ifStatement.AddCode($"return {entityIdVarName};");
                updateEntityMethod.AddCode(ifStatement);
            }

            updateEntityMethod.AddEmptyLine();
            updateEntityMethod.AddCode("throw new NotSupportedException();");

            ClassBuilder.AddMethod(updateEntityMethod);
            AddRequiredDeserializeMethods(namedTypeDescriptor);
        }
    }
}
