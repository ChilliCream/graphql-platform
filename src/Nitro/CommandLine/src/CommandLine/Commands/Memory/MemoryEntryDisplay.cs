using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Memory;

namespace ChilliCream.Nitro.CommandLine.Commands.Memory;

/// <summary>
/// Renders a <see cref="MemoryEntryResult"/> the same way in <c>search</c>
/// and <c>recent</c>, whichever collection it came from.
/// </summary>
internal static class MemoryEntryDisplay
{
    public static void WriteLine(INitroConsole console, MemoryEntryResult entry)
    {
        var typeLabel = entry.Type ?? MemoryCollections.Journal;
        var tagsSuffix = entry.Tags.Count > 0 ? $"  [{string.Join(", ", entry.Tags)}]" : "";
        var timestamp = entry.UpdatedAt ?? entry.CreatedAt;

        console.WriteLine($"{entry.Id}  {typeLabel}  {MemoryDates.Format(timestamp)}{tagsSuffix}");
    }
}
