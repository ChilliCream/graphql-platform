namespace ChilliCream.Nitro.CommandLine.Services.Memory;

/// <summary>
/// One tag in use, with how many curated memories carry it.
/// </summary>
internal sealed record MemoryTagCount(string Tag, int Count);
