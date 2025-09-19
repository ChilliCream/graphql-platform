using StrawberryShake.CodeGeneration.CSharp.Builders;

namespace StrawberryShake.CodeGeneration.CSharp;

internal static class MethodCallBuilderExtensions
{
    public static MethodCallBuilder AddArgumentRange(
        this MethodCallBuilder builder,
        IEnumerable<string> arguments)
    {
        foreach (var argument in arguments)
        {
            builder.AddArgument(argument);
        }

        return builder;
    }

    public static MethodCallBuilder Chain(
        this MethodCallBuilder builder,
        Action<MethodCallBuilder> configure)
    {
        var chainedMethod = MethodCallBuilder.Inline();
        configure(chainedMethod);
        builder.AddChainedCode(chainedMethod);
        return builder;
    }

    public static MethodCallBuilder If(
        this MethodCallBuilder builder,
        bool condition,
        Action<MethodCallBuilder> configure)
    {
        if (condition)
        {
            configure(builder);
        }

        return builder;
    }
}
