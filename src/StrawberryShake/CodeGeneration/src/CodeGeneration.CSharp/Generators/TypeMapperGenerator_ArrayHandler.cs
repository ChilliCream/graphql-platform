using StrawberryShake.CodeGeneration.CSharp.Builders;
using StrawberryShake.CodeGeneration.CSharp.Extensions;
using StrawberryShake.CodeGeneration.Descriptors.TypeDescriptors;
using static StrawberryShake.CodeGeneration.Utilities.NameUtils;

namespace StrawberryShake.CodeGeneration.CSharp.Generators;

public partial class TypeMapperGenerator
{
    private const string List = "list";
    private const string Child = "child";

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
            .AddParameter(List)
            .SetType(listTypeDescriptor.ToStateTypeReference());

        if (settings.IsStoreEnabled())
        {
            methodBuilder
                .AddParameter(Snapshot)
                .SetType(TypeNames.IEntityStoreSnapshot);
        }

        var listVarName = GetParameterName(listTypeDescriptor.Name) + "s";

        methodBuilder.AddCode(EnsureProperNullability(List, isNonNullable));

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
                    .AddCode($"{Child} in {List}"))
            .AddCode(
                MethodCallBuilder
                    .New()
                    .SetMethodName(listVarName, nameof(List<object>.Add))
                    .AddArgument(MethodCallBuilder
                        .Inline()
                        .SetMethodName(MapMethodNameFromTypeName(listTypeDescriptor.InnerType))
                        .AddArgument(Child)
                        .If(settings.IsStoreEnabled(), x => x.AddArgument(Snapshot))));

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
