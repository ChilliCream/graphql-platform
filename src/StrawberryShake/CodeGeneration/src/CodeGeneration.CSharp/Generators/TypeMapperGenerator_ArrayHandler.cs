using System.Collections.Generic;
using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.CSharp.Extensions;
using StrawberryShake.CodeGeneration.Extensions;

namespace StrawberryShake.CodeGeneration.CSharp
{
    public partial class TypeMapperGenerator
    {
        private const string ListParamName = "list";

        private void AddArrayHandler(
            ClassBuilder classBuilder,
            ConstructorBuilder constructorBuilder,
            MethodBuilder methodBuilder,
            ListTypeDescriptor listTypeDescriptor,
            HashSet<string> processed,
            bool isNonNullable)
        {
            methodBuilder.AddParameter(
                ParameterBuilder.New()
                    .SetType(listTypeDescriptor.ToEntityIdBuilder())
                    .SetName(ListParamName));
            var listVarName = listTypeDescriptor.Name.WithLowerFirstChar() + "s";

            if (!isNonNullable)
            {
                methodBuilder.AddCode(EnsureProperNullability(ListParamName, isNonNullable));
            }

            methodBuilder.AddCode(
                AssignmentBuilder.New()
                    .SetLefthandSide($"var {listVarName}")
                    .SetRighthandSide(
                        CodeBlockBuilder.New()
                            .AddCode("new ")
                            .AddCode(TypeNames.List)
                            .AddCode("<")
                            .AddCode(
                                listTypeDescriptor.InnerType.ToBuilder()
                                    .SkipTraliingSpace())
                            .AddCode(">")
                            .AddCode("()")));
            methodBuilder.AddEmptyLine();

            var loopbuilder = ForEachBuilder.New()
                .SetLoopHeader(
                    CodeBlockBuilder.New()
                        .AddCode(listTypeDescriptor.InnerType.ToEntityIdBuilder())
                        .AddCode($"child in {ListParamName}"))
                .AddCode(
                    MethodCallBuilder.New()
                        .SetPrefix($"{listVarName}.")
                        .SetMethodName("Add")
                        .AddArgument(
                            BuildMapMethodCall(
                                listTypeDescriptor.InnerType,
                                "child")));

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
