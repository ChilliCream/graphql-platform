using ChilliCream.Nitro.CommandLine.Commands.Agent.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Tasks;
using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Commands.Agent;

internal sealed class ListAgentCommand : Command
{
    /// <summary>
    /// The staleness threshold applied by <c>--stale</c>, mirroring
    /// <c>agent tasks stale</c>'s default of 30 days.
    /// </summary>
    private const int StaleDays = 30;

    public ListAgentCommand() : base("list")
    {
        Description = "List registered agents.";

        Options.Add(Opt<RoleAgentOption>.Instance);
        Options.Add(Opt<StaleAgentOption>.Instance);
        Options.Add(Opt<OptionalOutputFormatOption>.Instance);

        this.AddExamples("agent list", "agent list --role \"backend\"", "agent list --stale");

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static async Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken cancellationToken)
    {
        var console = services.GetRequiredService<INitroConsole>();
        var registry = services.GetRequiredService<IAgentRegistry>();
        var timeProvider = services.GetRequiredService<TimeProvider>();
        var resultHolder = services.GetRequiredService<IResultHolder>();

        var role = parseResult.GetValue(Opt<RoleAgentOption>.Instance);
        var stale = parseResult.GetValue(Opt<StaleAgentOption>.Instance);
        var staleBefore = stale ? timeProvider.GetUtcNow() - TimeSpan.FromDays(StaleDays) : (DateTimeOffset?)null;

        var agents = await registry.ListAsync(role, staleBefore, cancellationToken);

        if (!console.IsHumanReadable)
        {
            resultHolder.SetResult(
                new ListResult<AgentListRowResult>(agents.Select(ToRow).ToArray()));
            return ExitCodes.Success;
        }

        if (agents.Count == 0)
        {
            console.WriteLine("No registered agents.");
            return ExitCodes.Success;
        }

        foreach (var agent in agents)
        {
            var roleSuffix = agent.Role.Length > 0 ? $"  role {agent.Role}" : "";
            var implicitSuffix = agent.Implicit ? "  (implicit)" : "";

            console.WriteLine(
                $"{agent.Name}{roleSuffix}{implicitSuffix}  registered {TaskDates.Format(agent.RegisteredAt)}"
                + $"  last seen {TaskDates.Format(agent.LastSeenAt)}");
        }

        return ExitCodes.Success;
    }

    private static AgentListRowResult ToRow(AgentRecord agent)
        => new(agent.Name, agent.Role, agent.Implicit, agent.RegisteredAt, agent.LastSeenAt);

    public sealed record AgentListRowResult(
        string Name, string Role, bool Implicit, DateTimeOffset RegisteredAt, DateTimeOffset LastSeenAt);
}
