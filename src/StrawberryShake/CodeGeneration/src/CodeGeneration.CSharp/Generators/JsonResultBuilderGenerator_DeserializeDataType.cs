using System.Collections.Generic;
using StrawberryShake.CodeGeneration.CSharp.Builders;
using static StrawberryShake.CodeGeneration.NamingConventions;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public partial class JsonResultBuilderGenerator
    {
        private void AddDataTypeDeserializerMethod(
            ClassBuilder classBuilder,
            MethodBuilder methodBuilder,
            NamedTypeDescriptor namedTypeDescriptor,
            HashSet<string> processed)
        {
            var returnStatement = MethodCallBuilder.New()
                .SetPrefix("return new ")
                .SetMethodName(DataTypeNameFromTypeName(namedTypeDescriptor.Name));

            foreach (PropertyDescriptor property in namedTypeDescriptor.Properties)
            {
                returnStatement.AddArgument(BuildUpdateMethodCall(property));
            }

            methodBuilder.AddCode(returnStatement);

            AddRequiredDeserializeMethods(
                namedTypeDescriptor,
                classBuilder,
                processed);
        }
    }
}
