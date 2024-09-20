using System.Text.Json;
using HotChocolate.Utilities;
using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.Descriptors.TypeDescriptors;
using static StrawberryShake.CodeGeneration.Descriptors.NamingConventions;
using static StrawberryShake.CodeGeneration.Utilities.NameUtils;

namespace StrawberryShake.CodeGeneration.CSharp.Generators;

public partial class JsonResultBuilderGenerator
{
    private const string _typename = "typename";

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
            var returnStatement = MethodCallBuilder
                .New()
                .SetReturn()
                .SetNew()
                .SetMethodName(complexTypeDescriptor.Name);

            foreach (var property in complexTypeDescriptor.Properties)
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
                .SetLeftHandSide($"var {_typename}")
                .SetRightHandSide(MethodCallBuilder
                    .Inline()
                    .SetMethodName(
                        _obj,
                        "Value",
                        nameof(JsonElement.GetProperty))
                    .AddArgument(WellKnownNames.TypeName.AsStringToken())
                    .Chain(x => x.SetMethodName(nameof(JsonElement.GetString)))));

        // If the type is an interface
        foreach (var concreteType in interfaceTypeDescriptor.ImplementedBy)
        {
            var returnStatement = CreateBuildDataStatement(concreteType)
                .SetReturn();

            var ifStatement = IfBuilder
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

    private MethodCallBuilder CreateBuildDataStatement(ObjectTypeDescriptor concreteType)
    {
        var returnStatement = MethodCallBuilder
            .New()
            .SetNew()
            .SetMethodName(
                $"{concreteType.RuntimeType.Namespace}.State." +
                CreateDataTypeName(concreteType.Name))
            .AddArgument("typename");

        foreach (var property in concreteType.Properties)
        {
            if (property.Name.EqualsOrdinal(WellKnownNames.TypeName))
            {
                continue;
            }

            returnStatement.AddArgument(
                CodeBlockBuilder
                    .New()
                    .AddCode($"{GetParameterName(property.Name)}: ")
                    .AddCode(BuildUpdateMethodCall(property)));
        }

        return returnStatement;
    }
}
