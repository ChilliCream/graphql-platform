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
                .SetName(NamingConventions.DeserializerMethodNameFromTypeName(typeReference))
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
                returnStatement.AddArgument(BuildUpdateMethodCall(property));
            }

            dateDeserializer.AddCode(returnStatement);

            ClassBuilder.AddMethod(dateDeserializer);
            AddRequiredDeserializeMethods(typeReference.Type);
        }
    }
}
