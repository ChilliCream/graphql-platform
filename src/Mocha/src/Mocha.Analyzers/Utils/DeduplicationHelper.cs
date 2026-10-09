namespace Mocha.Analyzers.Utils;

/// <summary>
/// Collapses duplicate syntax information entries that share a generated method name.
/// A partial handler class that repeats its handler interface, or a partial saga that
/// repeats its base type, on more than one declaration part produces one entry per part.
/// Those entries differ only by source location, so record equality cannot collapse them,
/// and emitting one initializer per entry would produce duplicate method declarations.
/// </summary>
internal static class DeduplicationHelper
{
    /// <summary>
    /// Groups the entries by their fully qualified type name and keeps a single deterministic
    /// representative per group. The representative preference is: an entry that carries XML
    /// documentation first, then an entry with a non-null location, then the smallest location
    /// file path (ordinal), then the smallest location start line. The grouping preserves
    /// first-occurrence order, so for inputs without duplicates the output sequence is
    /// element-for-element identical to the input.
    /// </summary>
    /// <typeparam name="TInfo">The syntax information type being deduplicated.</typeparam>
    /// <param name="infos">The entries to deduplicate.</param>
    /// <param name="typeNameSelector">
    /// Selects the fully qualified <c>global::</c>-prefixed type name that keys each group.
    /// This is the same string the generated method name is salted on, so one group maps to
    /// one generated initializer method name.
    /// </param>
    /// <param name="xmlDocumentationSelector">Selects the XML documentation of an entry, if any.</param>
    /// <param name="locationSelector">Selects the source location of an entry, if any.</param>
    /// <returns>One representative entry per distinct type name, in first-occurrence order.</returns>
    public static IEnumerable<TInfo> SelectRepresentatives<TInfo>(
        IEnumerable<TInfo> infos,
        Func<TInfo, string> typeNameSelector,
        Func<TInfo, string?> xmlDocumentationSelector,
        Func<TInfo, LocationInfo?> locationSelector)
    {
        return infos
            .GroupBy(typeNameSelector, StringComparer.Ordinal)
            .Select(group => group
                .OrderBy(info => xmlDocumentationSelector(info) is null ? 1 : 0)
                .ThenBy(info => locationSelector(info) is null ? 1 : 0)
                .ThenBy(info => locationSelector(info)?.FilePath ?? string.Empty, StringComparer.Ordinal)
                .ThenBy(info => locationSelector(info)?.StartLine ?? 0)
                .First());
    }
}
