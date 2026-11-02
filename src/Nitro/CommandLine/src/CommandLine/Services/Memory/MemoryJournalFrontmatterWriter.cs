using System.Globalization;
using System.Text;

namespace ChilliCream.Nitro.CommandLine.Services.Memory;

/// <summary>
/// Serializes a <see cref="MemoryJournalFrontmatter"/> back into the
/// restricted frontmatter grammar <see cref="MemoryJournalFrontmatterParser"/>
/// reads, followed by the markdown body.
/// </summary>
internal static class MemoryJournalFrontmatterWriter
{
    public static string Write(MemoryJournalFrontmatter frontmatter)
    {
        var builder = new StringBuilder();

        builder.Append("---\n");
        builder.Append("schema: ").Append(frontmatter.Schema.ToString(CultureInfo.InvariantCulture)).Append('\n');
        builder.Append("id: ").Append(frontmatter.Id).Append('\n');
        builder.Append("created_at: ").Append(MemoryDates.Format(frontmatter.CreatedAt)).Append('\n');
        builder.Append("created_by: ").Append(frontmatter.CreatedBy).Append('\n');
        builder.Append("---\n");
        builder.Append(frontmatter.Body);

        return builder.ToString();
    }
}
