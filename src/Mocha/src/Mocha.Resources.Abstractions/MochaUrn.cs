using System.Text;

namespace Mocha.Resources;

/// <summary>
/// Helpers for building URN-shaped identifiers for <see cref="MochaResource"/> of the form
/// <c>urn:mocha:&lt;system&gt;:&lt;kind&gt;:&lt;segment&gt;[:&lt;segment&gt;...]</c>.
/// </summary>
public static class MochaUrn
{
    /// <summary>
    /// The constant URN scheme prefix used for every Mocha resource id (<c>urn:mocha:</c>).
    /// </summary>
    public const string Prefix = "urn:mocha:";

    /// <summary>
    /// Builds a URN of the form <c>urn:mocha:&lt;system&gt;:&lt;kind&gt;:&lt;segment&gt;[:&lt;segment&gt;...]</c>.
    /// </summary>
    /// <param name="system">The contributing system identifier (e.g. <c>rabbitmq</c>, <c>core</c>). Must be non-empty.</param>
    /// <param name="kind">The short kind name (e.g. <c>queue</c>, <c>handler</c>). Must be non-empty.</param>
    /// <param name="segments">Additional, ordered identifying segments. Each segment must be non-empty.</param>
    /// <returns>A deterministic URN string.</returns>
    /// <exception cref="ArgumentException">Thrown when any required argument is null, empty, or whitespace.</exception>
    public static string Create(string system, string kind, params ReadOnlySpan<string> segments)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(system);
        ArgumentException.ThrowIfNullOrWhiteSpace(kind);

        var builder = new StringBuilder(Prefix.Length + system.Length + 1 + kind.Length + 16);
        builder.Append(Prefix);
        AppendSegment(builder, system);
        builder.Append(':');
        AppendSegment(builder, kind);

        foreach (var segment in segments)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(segment);
            builder.Append(':');
            AppendSegment(builder, segment);
        }

        return builder.ToString();
    }

    private static void AppendSegment(StringBuilder builder, string segment)
    {
        for (var i = 0; i < segment.Length; i++)
        {
            var c = segment[i];
            if (c == ':')
            {
                builder.Append("%3A");
            }
            else
            {
                builder.Append(c);
            }
        }
    }
}
