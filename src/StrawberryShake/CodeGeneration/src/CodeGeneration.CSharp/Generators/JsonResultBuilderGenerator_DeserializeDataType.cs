using System;
using System.Linq;
using System.Threading.Tasks;
using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.Extensions;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public partial class JsonResultBuilderGenerator
    {
        private void AddDataTypeDeserializerMethod(TypeReferenceDescriptor typeReference)
        {
            var dateDeserializer = MethodBuilder.New()
                .SetReturnType(typeReference.TypeName)
                .SetName(TypeDeserializeMethodNameFromTypeName(typeReference))
                .AddParameter(ParameterBuilder.New().SetType("JsonElement").SetName(objParamName))
                .AddParameter(
                    ParameterBuilder.New().SetType($"ISet<{WellKnownNames.EntityId}>").SetName(EntityIdsParam)
                );

            dateDeserializer.AddCode(
                EnsureJsonValueIsNotNull(),
                !typeReference.IsNullable
            );

            var returnStatement = MethodCallBuilder.New()
                .SetPrefix("return new ")
                .SetMethodName(NamingConventions.DataTypeNameFromTypeName(typeReference.TypeName));

            foreach (NamedTypeReferenceDescriptor property in typeReference.Type.Properties)
            {
                // If the property type  is a list type, we must
                if (property.IsListType)
                {
                    var listVarName = property.Name.WithLowerFirstChar();
                    dateDeserializer.AddCode(
                        $"var {listVarName} = new List<{(property.IsEntityType ? WellKnownNames.EntityId : property.TypeName)}>();"
                    );

                    dateDeserializer.AddCode(
                        ForEachBuilder.New()
                            .SetLoopHeader(
                                $"JsonElement child in {objParamName}.GetProperty(\"{listVarName}\").EnumerateArray()"
                            )
                            .AddCode(
                                MethodCallBuilder.New()
                                    .SetPrefix($"{listVarName}.")
                                    .SetMethodName("Add")
                                    .AddArgument(BuildUpdateMethodCall(property, "child"))
                            )
                    );

                    dateDeserializer.AddEmptyLine();
                    returnStatement.AddArgument(listVarName);
                }
                else
                {
                    returnStatement.AddArgument(BuildUpdateMethodCall(property, objParamName));
                }
            }

            dateDeserializer.AddCode(returnStatement);

            ClassBuilder.AddMethod(dateDeserializer);
            AddRequiredDeserializeMethods(typeReference.Type);
        }
    }
}
