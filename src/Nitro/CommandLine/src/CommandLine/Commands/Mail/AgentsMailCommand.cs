using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Tasks;

namespace ChilliCream.Nitro.CommandLine.Commands.Mail;

internal sealed class AgentsMailCommand : Command
{
    public AgentsMailCommand() : base("agents")
    {
        Description = "List registered mail agents.";

        Options.Add(Opt<OptionalOutputFormatOption>.Instance);

        this.AddExamples("agent mail agents");

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static async Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken cancellationToken)
    {
        var console = services.GetRequiredService<INitroConsole>();
        var store = services.GetRequiredService<IMailStore>();
        var resultHolder = services.GetRequiredService<IResultHolder>();

        var agents = await store.GetAgentsAsync(cancellationToken);

        if (!console.IsHumanReadable)
        {
            resultHolder.SetResult(new ListResult<MailAgent>(agents));
            return ExitCodes.Success;
        }

        if (agents.Count == 0)
        {
            console.WriteLine("No registered agents.");
            return ExitCodes.Success;
        }

        foreach (var agent in agents)
        {
            console.WriteLine(
                $"{agent.Name}  registered {TaskDates.Format(agent.RegisteredAt)}"
                + $"  last seen {TaskDates.Format(agent.LastSeenAt)}");
        }

        return ExitCodes.Success;
    }
}
