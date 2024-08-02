using System.Text;

namespace StrawberryShake.CodeGeneration.CSharp;

internal static class StringBuilderExtensions
{
    public static StringBuilder AppendLineForEach<T>(
        this StringBuilder builder,
        IEnumerable<T> collection,
        Func<T, string> factory)
    {
        foreach (var item in collection)
        {
            builder.AppendLine(factory(item));
        }

        return builder;
    }
}
