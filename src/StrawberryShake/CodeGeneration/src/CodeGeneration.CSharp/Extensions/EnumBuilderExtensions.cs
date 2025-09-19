using StrawberryShake.CodeGeneration.CSharp.Builders;

namespace StrawberryShake.CodeGeneration.CSharp;

internal static class EnumBuilderExtensions
{
    public static EnumBuilder AddElements(
        this EnumBuilder builder,
        IEnumerable<string> elements)
    {
        foreach (var element in elements)
        {
            builder.AddElement(element);
        }

        return builder;
    }
}
