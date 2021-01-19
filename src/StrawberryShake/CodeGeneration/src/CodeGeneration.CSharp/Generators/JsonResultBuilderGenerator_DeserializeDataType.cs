using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.Extensions;
using static StrawberryShake.CodeGeneration.NamingConventions;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public partial class JsonResultBuilderGenerator
    {
        private void AddDataTypeDeserializerMethod(NamedTypeDescriptor namedTypeDescriptor)
        {
            var dateDeserializer = MethodBuilder.New()
                .SetReturnType(namedTypeDescriptor.Name)
                .SetName(DeserializerMethodNameFromTypeName(namedTypeDescriptor))
                .AddParameter(ParameterBuilder.New()
                    .SetType(_jsonElementParamName)
                    .SetName(_objParamName))
                .AddParameter(ParameterBuilder.New()
                    .SetType($"ISet<{WellKnownNames.EntityId}>")
                    .SetName(_entityIdsParam));

            dateDeserializer.AddCode(
                EnsureJsonValueIsNotNull(),
                !namedTypeDescriptor.IsNullableType());

            var returnStatement = MethodCallBuilder.New()
                .SetPrefix("return new ")
                .SetMethodName(DataTypeNameFromTypeName(namedTypeDescriptor.Name));

            foreach (PropertyDescriptor property in namedTypeDescriptor.Properties)
            {
                returnStatement.AddArgument(BuildUpdateMethodCall(property));
            }

            dateDeserializer.AddCode(returnStatement);
            ClassBuilder.AddMethod(dateDeserializer);
            AddRequiredDeserializeMethods(namedTypeDescriptor);
        }
    }
}
