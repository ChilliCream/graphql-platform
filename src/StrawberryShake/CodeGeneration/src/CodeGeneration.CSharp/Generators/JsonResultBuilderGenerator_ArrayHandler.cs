using System.Collections.Generic;
using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.CSharp.Extensions;
using static StrawberryShake.CodeGeneration.Utilities.NameUtils;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public partial class JsonResultBuilderGenerator
    {
        private const string _child = "child";

        private void AddArrayHandler(
            ClassBuilder classBuilder,
            MethodBuilder methodBuilder,
            ListTypeDescriptor listTypeDescriptor,
            HashSet<string> processed)
        {
            var listVarName = GetParameterName(listTypeDescriptor.Name) + "s";

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
                                        .ToEntityIdBuilder()
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
                                        CodeInlineBuilder.From(_child)))))
                .AddEmptyLine()
                .AddCode($"return {listVarName};");

            AddDeserializeMethod(listTypeDescriptor.InnerType, classBuilder, processed);
        }
    }
}
