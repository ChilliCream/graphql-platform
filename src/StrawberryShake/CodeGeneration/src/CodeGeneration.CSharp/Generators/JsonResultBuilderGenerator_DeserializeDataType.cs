using System;
using System.Collections.Generic;
using System.Text.Json;
using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.Descriptors.TypeDescriptors;
using static StrawberryShake.CodeGeneration.Descriptors.NamingConventions;
using static StrawberryShake.CodeGeneration.Utilities.NameUtils;

namespace StrawberryShake.CodeGeneration.CSharp.Generators
{
    public partial class JsonResultBuilderGenerator
    {
        private const string _typename = "typename";
        private const string __typename = "__typename";

        private void AddDataTypeDeserializerMethod(
            ClassBuilder classBuilder,
            MethodBuilder methodBuilder,
            ComplexTypeDescriptor complexTypeDescriptor,
            HashSet<string> processed)
        {
            if (complexTypeDescriptor is InterfaceTypeDescriptor interfaceTypeDescriptor)
            {
                AddInterfaceDataTypeDeserializerToMethod(methodBuilder, interfaceTypeDescriptor);
            }
            else
            {
                MethodCallBuilder returnStatement = MethodCallBuilder
                    .New()
                    .SetReturn()
                    .SetNew()
                    .SetMethodName(complexTypeDescriptor.Name);

                foreach (PropertyDescriptor property in complexTypeDescriptor.Properties)
                {
                    returnStatement.AddArgument(BuildUpdateMethodCall(property));
                }

                methodBuilder.AddCode(returnStatement);
            }

            AddRequiredDeserializeMethods(complexTypeDescriptor, classBuilder, processed);
        }

        private void AddInterfaceDataTypeDeserializerToMethod(
            MethodBuilder methodBuilder,
            InterfaceTypeDescriptor interfaceTypeDescriptor)
        {
            methodBuilder.AddCode(
                AssignmentBuilder
                    .New()
                    .SetLefthandSide($"var {_typename}")
                    .SetRighthandSide(MethodCallBuilder
                        .Inline()
                        .SetMethodName(
                            _obj,
                            "Value",
                            nameof(JsonElement.GetProperty))
                        .AddArgument(__typename.AsStringToken())
                        .Chain(x => x.SetMethodName(nameof(JsonElement.GetString)))));

            // If the type is an interface
            foreach (ObjectTypeDescriptor concreteType in interfaceTypeDescriptor.ImplementedBy)
            {
                MethodCallBuilder returnStatement = MethodCallBuilder
                    .New()
                    .SetReturn()
                    .SetNew()
                    .SetMethodName(
                        $"{concreteType.RuntimeType.Namespace}.State." +
                        CreateDataTypeName(concreteType.Name))
                    .AddArgument("typename");

                foreach (PropertyDescriptor property in concreteType.Properties)
                {
                    if (property.Name.Value is __typename)
                    {
                        continue;
                    }

                    returnStatement.AddArgument(
                        CodeBlockBuilder
                            .New()
                            .AddCode($"{GetParameterName(property.Name)}: ")
                            .AddCode(BuildUpdateMethodCall(property)));
                }

                IfBuilder ifStatement = IfBuilder
                    .New()
                    .SetCondition(
                        $"typename?.Equals(\"{concreteType.Name}\", " +
                        $"{TypeNames.OrdinalStringComparison}) ?? false")
                    .AddCode(returnStatement);

                methodBuilder
                    .AddEmptyLine()
                    .AddCode(ifStatement);
            }

            methodBuilder
                .AddEmptyLine()
                .AddCode(ExceptionBuilder.New(TypeNames.NotSupportedException));
        }
    }
}
