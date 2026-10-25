using System.Text;

namespace ChilliCream.Nitro.CommandLine.Services.Memory;

/// <summary>
/// Renders curated memories as text blocks joined by <see cref="Separator"/>.
/// Each block contains the id, type, any tags, and the body.
/// </summary>
internal static class MemoryContextRenderer
{
    public const string Separator = "\n\n---\n\n";

    public static string RenderEntry(MemoryRecord record)
    {
        var builder = new StringBuilder();

        builder.Append(record.Id).Append(" (").Append(record.Type).Append(')');

        if (record.Tags.Count > 0)
        {
            builder.Append('\n').Append("Tags: ").Append(string.Join(", ", record.Tags));
        }

        builder.Append('\n').Append('\n').Append(record.Body);

        return builder.ToString();
    }

    public static string Render(IReadOnlyList<MemoryRecord> records)
        => string.Join(Separator, records.Select(RenderEntry));
}
