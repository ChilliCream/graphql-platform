using System;
using System.Linq;
using System.Threading.Tasks;
using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.Extensions;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public partial class JsonResultBuilderGenerator
    {
        private void AddDataTypeDeserializerMethod(TypeDescriptor typeDescriptor)
        {
            var dateDeserializer = MethodBuilder.New()
                .SetReturnType(typeDescriptor.Name)
                .SetName(DeserializerMethodNameFromTypeName(typeDescriptor))
                .AddParameter(ParameterBuilder.New().SetType("JsonElement").SetName(objParamName))
                .AddParameter(
                    ParameterBuilder.New().SetType($"ISet<{WellKnownNames.EntityId}>").SetName(EntityIdsParam)
                );

            dateDeserializer.AddCode(
                EnsureJsonValueIsNotNull(),
                !typeDescriptor.IsNullable
            );

            var returnStatement = MethodCallBuilder.New()
                .SetPrefix("return new ")
                .SetMethodName(NamingConventions.DataTypeNameFromTypeName(typeDescriptor.Name));

            foreach (NamedTypeReferenceDescriptor property in typeDescriptor.Properties)
            {
                returnStatement.AddArgument(BuildUpdateMethodCall(property));
            }


            dateDeserializer.AddCode(returnStatement);
            ClassBuilder.AddMethod(dateDeserializer);
            AddRequiredDeserializeMethods(typeDescriptor);
        }
    }
}
