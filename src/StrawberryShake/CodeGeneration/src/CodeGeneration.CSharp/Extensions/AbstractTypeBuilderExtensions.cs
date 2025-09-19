using StrawberryShake.CodeGeneration.CSharp.Builders;

namespace StrawberryShake.CodeGeneration.CSharp;

internal static class AbstractTypeBuilderExtensions
{
    public static AbstractTypeBuilder AddImplementsRange(
        this AbstractTypeBuilder builder,
        IEnumerable<string> range)
    {
        foreach (var implements in range)
        {
            builder.AddImplements(implements);
        }

        return builder;
    }
}
