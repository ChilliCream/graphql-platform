using System.Globalization;
using ChilliCream.Nitro.CommandLine.Commands.Agent.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Commands.Agent;

internal sealed class ListAgentCommand : Command
{
    public ListAgentCommand() : base("list")
    {
        Description = "List the actors this workspace knows, with their session when they have one.";

        Options.Add(Opt<RoleAgentOption>.Instance);
        Options.Add(Opt<OptionalOutputFormatOption>.Instance);

        this.AddExamples("agent list", "agent list --role \"orchestrator\"");

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static async Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken cancellationToken)
    {
        var console = services.GetRequiredService<INitroConsole>();
        var agents = services.GetRequiredService<IAgentStore>();
        var timeProvider = services.GetRequiredService<TimeProvider>();
        var resultHolder = services.GetRequiredService<IResultHolder>();

        var role = parseResult.GetValue(Opt<RoleAgentOption>.Instance);
        var now = timeProvider.GetUtcNow();

        var rows = await agents.ListAsync(cancellationToken);

        if (role is not null)
        {
            var normalizedRole = AgentRole.Normalize(role);
            rows = rows.Where(row => row.Role == normalizedRole).ToArray();
        }

        var ordered = Order(rows, now);

        if (!console.IsHumanReadable)
        {
            resultHolder.SetResult(
                new ListResult<AgentListRowResult>(ordered.Select(row => ToRow(row, now)).ToArray()));

            return ExitCodes.Success;
        }

        if (ordered.Count == 0)
        {
            console.WriteLine("No actors.");
            return ExitCodes.Success;
        }

        var lines = ordered.Select(row => AgentListLine.From(row, now)).ToArray();
        var widths = ColumnWidths.Compute(lines);

        foreach (var line in lines)
        {
            console.WriteLine(line.Format(widths));
        }

        return ExitCodes.Success;
    }

    /// <summary>
    /// Orders rows the way the board does: online agents first, then unreachable, then
    /// offline, with ties broken by the most recently seen and then by name.
    /// </summary>
    private static IReadOnlyList<AgentRow> Order(IReadOnlyList<AgentRow> rows, DateTimeOffset now)
        => rows
            .OrderBy(row => (int)AgentStateResolver.Resolve(row, now))
            .ThenByDescending(row => row.LastSeenAt)
            .ThenBy(row => row.Name, StringComparer.Ordinal)
            .ToArray();

    private static AgentListRowResult ToRow(AgentRow row, DateTimeOffset now) => new(
        row.Name,
        row.Role,
        row.Harness,
        row.StartedAt,
        row.LastSeenAt,
        AgentStateResolver.Resolve(row, now) == AgentState.Online);

    /// <summary>
    /// Formats the elapsed time between <paramref name="value"/> and <paramref name="now"/>
    /// as a short relative age: "now" under a minute, then minutes, hours, or days, falling
    /// back to an ISO date once the value is a week or older. A non-positive elapsed time
    /// (a clock-skewed future timestamp) also formats as "now".
    /// </summary>
    private static string FormatAge(DateTimeOffset value, DateTimeOffset now)
    {
        var elapsed = now - value;

        if (elapsed < TimeSpan.FromMinutes(1))
        {
            return "now";
        }

        if (elapsed < TimeSpan.FromHours(1))
        {
            return $"{(int)elapsed.TotalMinutes}m";
        }

        if (elapsed < TimeSpan.FromHours(24))
        {
            return $"{(int)elapsed.TotalHours}h";
        }

        if (elapsed < TimeSpan.FromDays(7))
        {
            return $"{(int)elapsed.TotalDays}d";
        }

        return value.ToUniversalTime().ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// One agent's human-readable columns, ready to be padded against the other rows
    /// printed alongside it.
    /// </summary>
    private readonly record struct AgentListLine(
        string Name, string Role, string Harness, string Started, string LastSeen, bool Online)
    {
        public static AgentListLine From(AgentRow row, DateTimeOffset now) => new(
            row.Name,
            row.Role.Length > 0 ? row.Role : "-",
            AgentHarnessDisplay.Name(row.Harness),
            FormatAge(row.StartedAt, now),
            FormatAge(row.LastSeenAt, now),
            AgentStateResolver.Resolve(row, now) == AgentState.Online);

        public string Format(ColumnWidths widths)
            => $"{Name.PadRight(widths.Name)}  {Role.PadRight(widths.Role)}  "
                + $"{Harness.PadRight(widths.Harness)}  {Started.PadRight(widths.Started)}  "
                + $"{LastSeen.PadRight(widths.LastSeen)}  {(Online ? "yes" : "no")}";
    }

    private readonly record struct ColumnWidths(int Name, int Role, int Harness, int Started, int LastSeen)
    {
        public static ColumnWidths Compute(IReadOnlyList<AgentListLine> lines) => new(
            lines.Max(line => line.Name.Length),
            lines.Max(line => line.Role.Length),
            lines.Max(line => line.Harness.Length),
            lines.Max(line => line.Started.Length),
            lines.Max(line => line.LastSeen.Length));
    }

    public sealed record AgentListRowResult(
        string Name,
        string Role,
        string? Harness,
        DateTimeOffset StartedAt,
        DateTimeOffset LastSeenAt,
        bool Online);
}
