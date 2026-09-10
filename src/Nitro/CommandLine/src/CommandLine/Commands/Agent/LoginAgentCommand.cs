using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Commands.Agent;

/// <summary>
/// Allocates an actor name for a harness Nitro has no session-start hook
/// for. The name belongs to no session until <c>agent register --actor</c>
/// binds it to one.
/// </summary>
internal sealed class LoginAgentCommand : Command
{
    public LoginAgentCommand() : base("login")
    {
        Description = "Allocate an actor name for a harness without a session-start hook.";

        Options.Add(Opt<OptionalOutputFormatOption>.Instance);

        this.AddExamples("agent login");

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static async Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken cancellationToken)
    {
        var console = services.GetRequiredService<INitroConsole>();
        var registry = services.GetRequiredService<IAgentRegistry>();
        var resultHolder = services.GetRequiredService<IResultHolder>();

        var agent = await registry.AllocateAsync(cancellationToken);

        if (!console.IsHumanReadable)
        {
            resultHolder.SetResult(new ObjectResult(new AgentLoginResult(agent.Name)));

            return ExitCodes.Success;
        }

        console.OkLine($"Your Nitro actor is '{agent.Name}'.");
        console.WriteLine();
        console.WriteLine("Bind it to this session with:");
        console.WriteLine($"  nitro agent register --actor {agent.Name}");

        return ExitCodes.Success;
    }

    public sealed record AgentLoginResult(string Actor);
}
