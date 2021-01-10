using System;
using System.Linq;
using System.Threading.Tasks;
using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.Extensions;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public partial class JsonResultBuilderGenerator
    {
        private void AddUpdateEntityArrayMethod(NamedTypeReferenceDescriptor typeReference)
        {
            var updateEntityMethod = MethodBuilder.New()
                .SetAccessModifier(AccessModifier.Private)
                .SetName(NamingConventions.DeserializerMethodNameFromTypeName(typeReference))
                .SetReturnType($"IList<{WellKnownNames.EntityId}>")
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
                typeReference.ListType == ListType.List
            );

            var listVarName = typeReference.Name.WithLowerFirstChar();
            updateEntityMethod.AddCode(
                $"var {listVarName} = new List<{(typeReference.IsEntityType ? WellKnownNames.EntityId : typeReference.TypeName)}>();"
            );

            updateEntityMethod.AddCode(
                ForEachBuilder.New()
                    .SetLoopHeader(
                        $"JsonElement child in {objParamName}.GetProperty(\"{listVarName}\").EnumerateArray()"
                    )
                    .AddCode(EnsureJsonValueIsNotNull("child"), !typeReference.IsNullable)
                    .AddCode(
                        MethodCallBuilder.New()
                            .SetPrefix($"{listVarName}.")
                            .SetMethodName("Add")
                            .AddArgument(BuildUpdateMethodCall(typeReference, "child"))
                    )
            );

            updateEntityMethod.AddEmptyLine();
            updateEntityMethod.AddCode($"return {listVarName};");

            ClassBuilder.AddMethod(updateEntityMethod);
            AddRequiredDeserializeMethods(typeReference.Type);
        }
    }
}
