namespace ChilliCream.Nitro.CommandLine.Services.Memory;

/// <summary>
/// One tag in use, with how many curated memories carry it in each scope.
/// </summary>
internal sealed record MemoryTagCount(string Tag, int ProjectCount, int GlobalCount, int TotalCount);
