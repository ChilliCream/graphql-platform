using System.Collections.Generic;
using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.Extensions;
using static StrawberryShake.CodeGeneration.NamingConventions;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public partial class JsonResultBuilderGenerator
    {
        private void AddDataTypeDeserializerMethod(
            ClassBuilder classBuilder,
            MethodBuilder methodBuilder,
            ComplexTypeDescriptor complexTypeDescriptor,
            HashSet<string> processed)
        {
            if (complexTypeDescriptor is InterfaceTypeDescriptor interfaceTypeDescriptor)
            {
                methodBuilder.AddCode(
                    "var typename = obj.Value.GetProperty(\"__typename\").GetString();");

                // If the type is an interface
                foreach (ObjectTypeDescriptor concreteType in interfaceTypeDescriptor.ImplementedBy)
                {
                    methodBuilder.AddEmptyLine();
                    var ifStatement = IfBuilder.New()
                        .SetCondition(
                            $"typename?.Equals(\"{concreteType.Name}\", " +
                            $"{TypeNames.OrdinalStringComparison}) ?? false");

                    var dataTypeName = $"global::{concreteType.RuntimeType.Namespace}.State."
                    + DataTypeNameFromTypeName(concreteType.Name);

                    var returnStatement = MethodCallBuilder.New()
                        .SetPrefix("return new ")
                        .SetMethodName(dataTypeName);

                    returnStatement.AddArgument("typename");
                    foreach (PropertyDescriptor property in concreteType.Properties)
                    {
                        returnStatement.AddArgument(
                            CodeBlockBuilder.New()
                                .AddCode($"{property.Name.WithLowerFirstChar()}: ")
                                .AddCode(BuildUpdateMethodCall(property)));
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
                    .SetMethodName(complexTypeDescriptor.Name);

                foreach (PropertyDescriptor property in complexTypeDescriptor.Properties)
                {
                    returnStatement.AddArgument(BuildUpdateMethodCall(property));
                }

                methodBuilder.AddCode(returnStatement);
            }

            AddRequiredDeserializeMethods(
                complexTypeDescriptor,
                classBuilder,
                processed);
        }
    }
}
