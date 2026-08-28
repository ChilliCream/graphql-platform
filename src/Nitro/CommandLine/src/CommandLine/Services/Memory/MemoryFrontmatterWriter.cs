using System.Globalization;
using System.Text;

namespace ChilliCream.Nitro.CommandLine.Services.Memory;

/// <summary>
/// Serializes a <see cref="MemoryFrontmatter"/> back into the restricted
/// frontmatter grammar <see cref="MemoryFrontmatterParser"/> reads, followed
/// by the markdown body.
/// </summary>
internal static class MemoryFrontmatterWriter
{
    public static string Write(MemoryFrontmatter frontmatter)
    {
        var builder = new StringBuilder();

        builder.Append("---\n");
        builder.Append("schema: ").Append(frontmatter.Schema.ToString(CultureInfo.InvariantCulture)).Append('\n');
        builder.Append("id: ").Append(frontmatter.Id).Append('\n');
        builder.Append("type: ").Append(frontmatter.Type).Append('\n');
        builder.Append("tags: [").Append(string.Join(", ", frontmatter.Tags)).Append("]\n");
        builder.Append("created_at: ").Append(MemoryDates.Format(frontmatter.CreatedAt)).Append('\n');
        builder.Append("updated_at: ").Append(MemoryDates.Format(frontmatter.UpdatedAt)).Append('\n');
        builder.Append("created_by: ").Append(frontmatter.CreatedBy).Append('\n');

        if (frontmatter.PromotedFrom is { } promotedFrom)
        {
            builder.Append("promoted_from: ").Append(promotedFrom).Append('\n');
        }

        builder.Append("---\n");
        builder.Append(frontmatter.Body);

        return builder.ToString();
    }
}
