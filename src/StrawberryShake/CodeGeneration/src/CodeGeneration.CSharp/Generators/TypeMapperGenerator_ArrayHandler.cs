using System.Collections.Generic;
using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.CSharp.Extensions;
using StrawberryShake.CodeGeneration.Descriptors.TypeDescriptors;
using static StrawberryShake.CodeGeneration.Utilities.NameUtils;

namespace StrawberryShake.CodeGeneration.CSharp.Generators
{
    public partial class TypeMapperGenerator
    {
        private const string _list = "list";
        private const string _child = "child";

        private void AddArrayHandler(
            ClassBuilder classBuilder,
            ConstructorBuilder constructorBuilder,
            MethodBuilder methodBuilder,
            ListTypeDescriptor listTypeDescriptor,
            HashSet<string> processed,
            bool isNonNullable)
        {
            methodBuilder
                .AddParameter(_list)
                .SetType(listTypeDescriptor.ToStateTypeReference());

            var listVarName = GetParameterName(listTypeDescriptor.Name) + "s";

            if (!isNonNullable)
            {
                methodBuilder.AddCode(EnsureProperNullability(_list, isNonNullable));
            }

            methodBuilder.AddCode(
                AssignmentBuilder
                    .New()
                    .SetLefthandSide($"var {listVarName}")
                    .SetRighthandSide(
                        CodeBlockBuilder
                            .New()
                            .AddCode("new ")
                            .AddCode(TypeNames.List)
                            .AddCode("<")
                            .AddCode(listTypeDescriptor.InnerType.ToTypeReference().SkipTrailingSpace())
                            .AddCode(">")
                            .AddCode("()")));
            methodBuilder.AddEmptyLine();

            ForEachBuilder forEachBuilder = ForEachBuilder
                .New()
                .SetLoopHeader(
                    CodeBlockBuilder
                        .New()
                        .AddCode(listTypeDescriptor.InnerType.ToStateTypeReference())
                        .AddCode($"{_child} in {_list}"))
                .AddCode(
                    MethodCallBuilder
                        .New()
                        .SetMethodName(listVarName, nameof(List<object>.Add))
                        .AddArgument(MethodCallBuilder
                            .Inline()
                            .SetMethodName(MapMethodNameFromTypeName(listTypeDescriptor.InnerType))
                            .AddArgument(_child)));

            methodBuilder
                .AddCode(forEachBuilder)
                .AddEmptyLine()
                .AddCode($"return {listVarName};");

            AddMapMethod(
                listVarName,
                listTypeDescriptor.InnerType,
                classBuilder,
                constructorBuilder,
                processed);
        }
    }
}
