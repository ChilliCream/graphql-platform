namespace ChilliCream.Nitro.CommandLine.Services.Memory;

/// <summary>
/// The outcome of rebuilding one scope's search index, as returned by the
/// <c>reindex</c> command.
/// </summary>
internal sealed record MemoryIndexRebuildResult(string Scope, int IndexedCount, string IndexPath);
