using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.Extensions;
using static StrawberryShake.CodeGeneration.NamingConventions;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public partial class JsonResultBuilderGenerator
    {
        private void AddDataTypeDeserializerMethod(
            NamedTypeDescriptor namedTypeDescriptor,
            ITypeDescriptor originalTypeDescriptor,
            ClassBuilder classBuilder)
        {
            var dateDeserializer = MethodBuilder.New()
                .SetReturnType(namedTypeDescriptor.Name)
                .SetName(DeserializerMethodNameFromTypeName(originalTypeDescriptor))
                .AddParameter(ParameterBuilder.New()
                    .SetType(_jsonElementParamName)
                    .SetName(_objParamName))
                .AddParameter(ParameterBuilder.New()
                    .SetType($"ISet<{TypeNames.EntityId}>")
                    .SetName(_entityIdsParam));

            dateDeserializer.AddCode(
                EnsureJsonValueIsNotNull(),
                originalTypeDescriptor.IsNonNullableType());

            var returnStatement = MethodCallBuilder.New()
                .SetPrefix("return new ")
                .SetMethodName(DataTypeNameFromTypeName(namedTypeDescriptor.Name));

            foreach (PropertyDescriptor property in namedTypeDescriptor.Properties)
            {
                returnStatement.AddArgument(BuildUpdateMethodCall(property));
            }

            dateDeserializer.AddCode(returnStatement);
            classBuilder.AddMethod(dateDeserializer);
            AddRequiredDeserializeMethods(namedTypeDescriptor, classBuilder);
        }
    }
}
