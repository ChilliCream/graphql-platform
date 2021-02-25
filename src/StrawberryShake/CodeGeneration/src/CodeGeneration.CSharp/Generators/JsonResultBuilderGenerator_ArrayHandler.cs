using System.Collections.Generic;
using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.CSharp.Extensions;
using static StrawberryShake.CodeGeneration.Utilities.NameUtils;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public partial class JsonResultBuilderGenerator
    {
        private void AddArrayHandler(
            ClassBuilder classBuilder,
            MethodBuilder methodBuilder,
            ListTypeDescriptor listTypeDescriptor,
            HashSet<string> processed)
        {
            var listVarName = GetParameterName(listTypeDescriptor.Name) + "s";

            methodBuilder.AddCode(
                AssignmentBuilder.New()
                    .SetLefthandSide($"var {listVarName}")
                    .SetRighthandSide(
                        CodeBlockBuilder.New()
                            .AddCode("new ")
                            .AddCode(TypeNames.List)
                            .AddCode("<")
                            .AddCode(listTypeDescriptor.InnerType.ToEntityIdBuilder()
                                .SkipTrailingSpace())
                            .AddCode(">")
                            .AddCode("()")
                    ));
            methodBuilder.AddEmptyLine();

            methodBuilder.AddCode(
                ForEachBuilder.New()
                    .SetLoopHeader(
                        $"{TypeNames.JsonElement} child in {_objParamName}.Value.EnumerateArray()")
                    .AddCode(
                        MethodCallBuilder.New()
                            .SetPrefix($"{listVarName}.")
                            .SetMethodName("Add")
                            .AddArgument(
                                BuildUpdateMethodCall(listTypeDescriptor.InnerType, "child"))));

            methodBuilder.AddEmptyLine();
            methodBuilder.AddCode($"return {listVarName};");

            AddDeserializeMethod(listTypeDescriptor.InnerType, classBuilder, processed);
        }
    }
}
