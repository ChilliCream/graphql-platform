namespace ChilliCream.Nitro.CommandLine.Services.Tasks;

/// <summary>
/// One elementary cycle among blocking dependencies, as returned by the
/// structured (JSON) output of <c>agent tasks dep cycles</c>. <see cref="Tasks"/>
/// lists the cycle's members in order; it wraps back around to its first
/// entry.
/// </summary>
internal sealed record TaskCycleResult(IReadOnlyList<string> Tasks);
