using StrawberryShake.CodeGeneration.CSharp.Builders;

namespace StrawberryShake.CodeGeneration.CSharp;

internal static class CodeBlockBuilderExtensions
{
    public static CodeBlockBuilder AddLine(
        this CodeBlockBuilder builder,
        string code)
    {
        return builder.AddCode(CodeLineBuilder.From(code));
    }

    public static CodeBlockBuilder AddMethodCall(
        this CodeBlockBuilder builder,
        Action<MethodCallBuilder> configure)
    {
        var methodCallBuilder = MethodCallBuilder.New();
        configure(methodCallBuilder);
        return builder.AddCode(methodCallBuilder);
    }

    public static MethodCallBuilder AddMethodCall(this CodeBlockBuilder builder)
    {
        var methodCallBuilder = MethodCallBuilder.New();
        builder.AddCode(methodCallBuilder);
        return methodCallBuilder;
    }

    public static AssignmentBuilder AddAssignment(
        this CodeBlockBuilder builder,
        string? assignedTo = null)
    {
        var assignmentBuilder = AssignmentBuilder
            .New();

        if (assignedTo is not null)
        {
            assignmentBuilder.SetLeftHandSide(assignedTo);
        }

        builder.AddCode(assignmentBuilder);

        return assignmentBuilder;
    }

    public static CodeBlockBuilder ForEach<T>(
        this CodeBlockBuilder codeBlockBuilder,
        IEnumerable<T> enumerable,
        Action<CodeBlockBuilder, T> configure)
    {
        foreach (var element in enumerable)
        {
            configure(codeBlockBuilder, element);
        }

        return codeBlockBuilder;
    }

    public static CodeBlockBuilder AddIf(
        this CodeBlockBuilder builder,
        Action<IfBuilder> configure)
    {
        var ifBuilder = IfBuilder.New();
        configure(ifBuilder);
        return builder.AddCode(ifBuilder);
    }

    public static CodeBlockBuilder If(
        this CodeBlockBuilder builder,
        bool condition,
        Action<CodeBlockBuilder> configure)
    {
        if (condition)
        {
            configure(builder);
        }

        return builder;
    }

    public static CodeBlockBuilder ArgumentException(
        this CodeBlockBuilder builder,
        string argumentName,
        string condition)
    {
        return builder.AddIf(x =>
            x.SetCondition(condition)
                .AddCode($"throw new {TypeNames.ArgumentException}(nameof({argumentName}));"));
    }

    public static ArrayBuilder AddArray(this CodeBlockBuilder method)
    {
        var arrayBuilder = ArrayBuilder.New();
        method.AddCode(arrayBuilder);
        return arrayBuilder;
    }
}
