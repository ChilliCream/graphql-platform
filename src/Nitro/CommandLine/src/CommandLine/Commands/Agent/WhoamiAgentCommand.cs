using ChilliCream.Nitro.CommandLine.Commands.Mail.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Commands.Agent;

internal sealed class WhoamiAgentCommand : Command
{
    public WhoamiAgentCommand() : base("whoami")
    {
        Description = "Print the resolved actor identity and whether it is registered in this workspace.";

        Options.Add(Opt<MailActorOption>.Instance);
        Options.Add(Opt<OptionalOutputFormatOption>.Instance);

        this.AddExamples("agent whoami");

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static async Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken cancellationToken)
    {
        var console = services.GetRequiredService<INitroConsole>();
        var registry = services.GetRequiredService<IAgentRegistry>();
        var environmentVariableProvider = services.GetRequiredService<IEnvironmentVariableProvider>();
        var resultHolder = services.GetRequiredService<IResultHolder>();

        var actor = MailActor.Resolve(
            parseResult.GetValue(Opt<MailActorOption>.Instance), environmentVariableProvider);

        var agent = await registry.GetAsync(actor, cancellationToken);
        var registered = agent?.Implicit == false;
        var role = registered ? agent?.Role ?? "" : "";
        var client = registered ? agent?.Client ?? "" : "";

        if (!console.IsHumanReadable)
        {
            resultHolder.SetResult(
                new ObjectResult(new AgentWhoamiResult(actor, registered, role, client)));
            return ExitCodes.Success;
        }

        console.WriteLine(actor);
        console.WriteLine(
            registered
                ? "Registered in this workspace."
                : "Not registered in this workspace. Run `nitro agent register` to register.");

        return ExitCodes.Success;
    }

    public sealed record AgentWhoamiResult(string Name, bool Registered, string Role, string Client);
}
