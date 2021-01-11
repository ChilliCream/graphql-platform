using System;
using System.Linq;
using System.Threading.Tasks;
using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.Extensions;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public partial class JsonResultBuilderGenerator
    {
        private void AddUpdateEntityMethod(TypeDescriptor typeDescriptor)
        {
            var updateEntityMethod = MethodBuilder.New()
                .SetAccessModifier(AccessModifier.Private)
                .SetName(DeserializerMethodNameFromTypeName(typeDescriptor))
                .SetReturnType(WellKnownNames.EntityId)
                .AddParameter(
                    ParameterBuilder.New()
                        .SetType("JsonElement")
                        .SetName(objParamName)
                )
                .AddParameter(
                    ParameterBuilder.New()
                        .SetType($"ISet<{WellKnownNames.EntityId}>")
                        .SetName(EntityIdsParam)
                );

            updateEntityMethod.AddCode(
                EnsureJsonValueIsNotNull(),
                !typeDescriptor.IsNullable
            );

            var entityIdVarName = "entityId";
            updateEntityMethod.AddCode(
                $"{WellKnownNames.EntityId} {entityIdVarName} = {ExtractIdFieldName}({objParamName});"
            );
            updateEntityMethod.AddCode($"{EntityIdsParam}.Add({entityIdVarName});");

            // If the type is an interface
            foreach (TypeDescriptor concreteType in typeDescriptor.IsImplementedBy)
            {
                updateEntityMethod.AddEmptyLine();
                var ifStatement = IfBuilder.New()
                    .SetCondition($"entityId.Name.Equals(\"{concreteType.Name}\", StringComparison.Ordinal)");

                var entityTypeName = NamingConventions.EntityTypeNameFromTypeName(concreteType.Name);

                var entityVarName = "entity";
                ifStatement.AddCode(
                    $"{entityTypeName} {entityTypeName} = {EntityStoreFieldName}.GetOrCreate<{entityTypeName}>({entityIdVarName});"
                );
                foreach (NamedTypeReferenceDescriptor property in concreteType.Properties)
                {
                    ifStatement.AddCode(
                        AssignmentBuilder.New()
                            .SetLefthandSide($"{entityVarName}.{property.Name}")
                            .SetRighthandSide(BuildUpdateMethodCall(property))
                    );
                }

                ifStatement.AddEmptyLine();
                ifStatement.AddCode($"return {entityIdVarName};");
                updateEntityMethod.AddCode(ifStatement);
            }


            updateEntityMethod.AddEmptyLine();
            updateEntityMethod.AddCode("throw new NotSupportedException();");

            ClassBuilder.AddMethod(updateEntityMethod);
            AddRequiredDeserializeMethods(typeDescriptor);
        }
    }
}
