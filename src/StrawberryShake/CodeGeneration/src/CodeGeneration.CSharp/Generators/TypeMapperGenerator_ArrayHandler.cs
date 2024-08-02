using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.CSharp.Extensions;
using StrawberryShake.CodeGeneration.Descriptors.TypeDescriptors;
using static StrawberryShake.CodeGeneration.Utilities.NameUtils;

namespace StrawberryShake.CodeGeneration.CSharp.Generators;

public partial class TypeMapperGenerator
{
    private const string _list = "list";
    private const string _child = "child";

    private static void AddArrayHandler(
        CSharpSyntaxGeneratorSettings settings,
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

        if (settings.IsStoreEnabled())
        {
            methodBuilder
                .AddParameter(_snapshot)
                .SetType(TypeNames.IEntityStoreSnapshot);
        }

        var listVarName = GetParameterName(listTypeDescriptor.Name) + "s";

        methodBuilder.AddCode(EnsureProperNullability(_list, isNonNullable));

        methodBuilder.AddCode(
            AssignmentBuilder
                .New()
                .SetLeftHandSide($"var {listVarName}")
                .SetRightHandSide(
                    CodeBlockBuilder
                        .New()
                        .AddCode("new ")
                        .AddCode(TypeNames.List)
                        .AddCode("<")
                        .AddCode(
                            listTypeDescriptor.InnerType.ToTypeReference().SkipTrailingSpace())
                        .AddCode(">")
                        .AddCode("()")));
        methodBuilder.AddEmptyLine();

        var forEachBuilder = ForEachBuilder
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
                        .AddArgument(_child)
                        .If(settings.IsStoreEnabled(), x => x.AddArgument(_snapshot))));

        methodBuilder
            .AddCode(forEachBuilder)
            .AddEmptyLine()
            .AddCode($"return {listVarName};");

        AddMapMethod(
            settings,
            listTypeDescriptor.InnerType,
            classBuilder,
            constructorBuilder,
            processed);
    }
}
