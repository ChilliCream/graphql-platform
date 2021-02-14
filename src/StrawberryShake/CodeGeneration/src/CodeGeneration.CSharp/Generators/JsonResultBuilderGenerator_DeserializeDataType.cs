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
            if (namedTypeDescriptor.IsInterface)
            {
                methodBuilder.AddCode("var typename = obj.Value.GetProperty(\"__typename\").GetString();");

                // If the type is an interface
                foreach (NamedTypeDescriptor concreteType in namedTypeDescriptor.ImplementedBy)
                {
                    methodBuilder.AddEmptyLine();
                    var ifStatement = IfBuilder.New()
                        .SetCondition(
                            $"typename?.Equals(\"{concreteType.GraphQLTypeName}\", " +
                            $"{TypeNames.OrdinalStringComparisson}) ?? false");

                    var dataTypeName = DataTypeNameFromTypeName(concreteType.Name);

                    var returnStatement = MethodCallBuilder.New()
                        .SetPrefix("return new ")
                        .SetMethodName(dataTypeName);

                    returnStatement.AddArgument("typename");
                    foreach (PropertyDescriptor property in namedTypeDescriptor.Properties)
                    {
                        returnStatement.AddArgument(BuildUpdateMethodCall(property));
                    }

                    ifStatement.AddCode(returnStatement);
                    methodBuilder.AddCode(ifStatement);
                }

                methodBuilder.AddEmptyLine();
                methodBuilder.AddCode($"throw new {TypeNames.NotSupportedException}();");
            }
            else
            {
                var returnStatement = MethodCallBuilder.New()
                    .SetPrefix("return new ")
                    .SetMethodName(namedTypeDescriptor.Name);

                foreach (PropertyDescriptor property in namedTypeDescriptor.Properties)
                {
                    returnStatement.AddArgument(BuildUpdateMethodCall(property));
                }

                methodBuilder.AddCode(returnStatement);
            }

            AddRequiredDeserializeMethods(
                namedTypeDescriptor,
                classBuilder,
                processed);
        }
    }
}
