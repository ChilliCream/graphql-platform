using System.Collections.Generic;
using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.CSharp.Extensions;
using static StrawberryShake.CodeGeneration.Utilities.NameUtils;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public partial class TypeMapperGenerator
    {
        private const string _listParameterName = "list";

        private void AddArrayHandler(
            ClassBuilder classBuilder,
            ConstructorBuilder constructorBuilder,
            MethodBuilder methodBuilder,
            ListTypeDescriptor listTypeDescriptor,
            HashSet<string> processed,
            bool isNonNullable)
        {
            methodBuilder.AddParameter(
                _listParameterName,
                x => x.SetType(listTypeDescriptor.ToEntityIdBuilder()));

            var listVarName = GetParameterName(listTypeDescriptor.Name) + "s";

            if (!isNonNullable)
            {
                methodBuilder.AddCode(EnsureProperNullability(_listParameterName, isNonNullable));
            }

            methodBuilder.AddCode(
                AssignmentBuilder.New()
                    .SetLefthandSide($"var {listVarName}")
                    .SetRighthandSide(
                        CodeBlockBuilder.New()
                            .AddCode("new ")
                            .AddCode(TypeNames.List)
                            .AddCode("<")
                            .AddCode(listTypeDescriptor.InnerType.ToBuilder().SkipTrailingSpace())
                            .AddCode(">")
                            .AddCode("()")));
            methodBuilder.AddEmptyLine();

            var loopbuilder = ForEachBuilder.New()
                .SetLoopHeader(
                    CodeBlockBuilder.New()
                        .AddCode(listTypeDescriptor.InnerType.ToEntityIdBuilder())
                        .AddCode($"child in {_listParameterName}"))
                .AddCode(
                    MethodCallBuilder.New()
                        .SetPrefix($"{listVarName}.")
                        .SetMethodName("Add")
                        .AddArgument(BuildMapMethodCall(listTypeDescriptor.InnerType, "child")));

            methodBuilder.AddCode(loopbuilder);
            methodBuilder.AddEmptyLine();
            methodBuilder.AddCode($"return {listVarName};");

            AddMapMethod(
                listVarName,
                listTypeDescriptor.InnerType,
                classBuilder,
                constructorBuilder,
                processed);
        }
    }
}
