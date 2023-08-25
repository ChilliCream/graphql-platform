using System.Collections.Generic;
using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.CSharp.Extensions;
using StrawberryShake.CodeGeneration.Descriptors.TypeDescriptors;
using StrawberryShake.CodeGeneration.Extensions;
using static StrawberryShake.CodeGeneration.Utilities.NameUtils;

namespace StrawberryShake.CodeGeneration.CSharp.Generators;

public partial class JsonResultBuilderGenerator
{
    private const string _child = "child";
    private const string _parsedValue = "parsedValue";

    private void AddArrayHandler(
        ClassBuilder classBuilder,
        MethodBuilder methodBuilder,
        ListTypeDescriptor listTypeDescriptor,
        HashSet<string> processed)
    {
        var listVarName = GetParameterName(listTypeDescriptor.Name) + "s";

        var listType = listTypeDescriptor.InnerType
            .ToStateTypeReference()
            .SkipTrailingSpace();

        if (listTypeDescriptor.InnerType.IsNonNull())
        {
            AddNonNullableInnerTypeBody(methodBuilder, listTypeDescriptor, listVarName);
        }
        else
        {
            AddNullableInnerTypeBody(methodBuilder, listTypeDescriptor, listVarName, listType);
        }


        AddDeserializeMethod(listTypeDescriptor.InnerType, classBuilder, processed);
    }

    private static void AddNonNullableInnerTypeBody(
        MethodBuilder methodBuilder,
        ListTypeDescriptor listTypeDescriptor,
        string listVarName)
    {
        methodBuilder
            .AddCode(
                AssignmentBuilder
                    .New()
                    .SetLefthandSide($"var {listVarName}")
                    .SetRighthandSide(
                        CodeBlockBuilder
                            .New()
                            .AddCode("new ")
                            .AddCode(TypeNames.List)
                            .AddCode("<")
                            .AddCode(
                                listTypeDescriptor.InnerType
                                    .ToStateTypeReference()
                                    .SkipTrailingSpace())
                            .AddCode(">")
                            .AddCode("()")))
            .AddEmptyLine()
            .AddCode(
                ForEachBuilder
                    .New()
                    .SetLoopHeader(
                        $"{TypeNames.JsonElement} {_child} in {_obj}.Value.EnumerateArray()")
                    .AddCode(
                        MethodCallBuilder
                            .New()
                            .SetMethodName(listVarName, nameof(List<object>.Add))
                            .AddArgument(
                                BuildUpdateMethodCall(
                                    listTypeDescriptor.InnerType,
                                    CodeInlineBuilder.From(_child),
                                    setNullForgiving: false))))
            .AddEmptyLine()
            .AddCode($"return {listVarName};");
    }

    private static void AddNullableInnerTypeBody(
        MethodBuilder methodBuilder,
        ListTypeDescriptor listTypeDescriptor,
        string listVarName, TypeReferenceBuilder listType)
    {
        methodBuilder
            .AddCode(
                AssignmentBuilder
                    .New()
                    .SetLefthandSide($"var {listVarName}")
                    .SetRighthandSide(
                        CodeBlockBuilder
                            .New()
                            .AddCode("new ")
                            .AddCode(TypeNames.List)
                            .AddCode("<")
                            .AddCode(listType)
                            .AddCode(">")
                            .AddCode("()")))
            .AddEmptyLine()
            .AddCode(
                ForEachBuilder
                    .New()
                    .SetLoopHeader(
                        $"{TypeNames.JsonElement} {_child} in {_obj}.Value.EnumerateArray()")
                    .AddCode(
                        CodeBlockBuilder
                            .New()
                            .AddCode(AssignmentBuilder
                                .New()
                                .SetLefthandSide($"{listType} {_parsedValue}")
                                .SetRighthandSide(BuildUpdateMethodCall(
                                    listTypeDescriptor.InnerType,
                                    CodeInlineBuilder.From(_child),
                                    setNullForgiving: false)))
                            .AddCode(IfBuilder
                                .New()
                                .SetCondition($"{_parsedValue} is not null")
                                .AddCode(MethodCallBuilder
                                    .New()
                                    .SetMethodName(listVarName, nameof(List<object>.Add))
                                    .AddArgument($"{_parsedValue}")))))
            .AddEmptyLine()
            .AddCode($"return {listVarName};");
    }
}
