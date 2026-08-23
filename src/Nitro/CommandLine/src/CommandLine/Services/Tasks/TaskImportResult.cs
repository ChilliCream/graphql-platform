namespace ChilliCream.Nitro.CommandLine.Services.Tasks;

/// <summary>
/// The outcome of importing sync records: how many replaced the stored task
/// state because they were at least as new, how many were skipped because
/// the stored task was newer, and how many records were read in total.
/// </summary>
internal sealed record TaskImportResult(int Applied, int Skipped, int Total);
