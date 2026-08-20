using ChilliCream.Nitro.CommandLine.Commands.Mail.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Mail;

namespace ChilliCream.Nitro.CommandLine.Commands.Mail;

internal sealed class WhoamiMailCommand : Command
{
    public WhoamiMailCommand() : base("whoami")
    {
        Description = "Print the resolved actor identity and whether it is registered in this workspace.";

        Options.Add(Opt<MailActorOption>.Instance);
        Options.Add(Opt<OptionalOutputFormatOption>.Instance);

        this.AddExamples("agent mail whoami");

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static async Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken cancellationToken)
    {
        var console = services.GetRequiredService<INitroConsole>();
        var store = services.GetRequiredService<IMailStore>();
        var environmentVariableProvider = services.GetRequiredService<IEnvironmentVariableProvider>();
        var resultHolder = services.GetRequiredService<IResultHolder>();

        var actor = MailActor.Resolve(
            parseResult.GetValue(Opt<MailActorOption>.Instance), environmentVariableProvider);

        var agent = await store.GetAgentAsync(actor, cancellationToken);
        var registered = agent is not null;

        if (!console.IsHumanReadable)
        {
            resultHolder.SetResult(new ObjectResult(new MailWhoamiResult(actor, registered)));
            return ExitCodes.Success;
        }

        console.WriteLine(actor);
        console.WriteLine(
            registered
                ? "Registered in this workspace."
                : "Not registered in this workspace. Run `nitro agent mail register` to register.");

        return ExitCodes.Success;
    }

    public sealed record MailWhoamiResult(string Name, bool Registered);
}
