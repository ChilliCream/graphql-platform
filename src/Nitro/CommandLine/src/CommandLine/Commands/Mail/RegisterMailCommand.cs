using ChilliCream.Nitro.CommandLine.Commands.Mail.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Mail;

namespace ChilliCream.Nitro.CommandLine.Commands.Mail;

internal sealed class RegisterMailCommand : Command
{
    public RegisterMailCommand() : base("register")
    {
        Description = "Register the resolved actor as a mail agent. "
            + "--actor is per invocation; set NITRO_MAIL_ACTOR to persist an identity.";

        Options.Add(Opt<MailActorOption>.Instance);
        Options.Add(Opt<OptionalOutputFormatOption>.Instance);

        this.AddExamples("agent mail register", "agent mail register --actor \"claude-1\"");

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

        var agent = await store.RegisterAgentAsync(actor, cancellationToken);

        if (!console.IsHumanReadable)
        {
            resultHolder.SetResult(new ObjectResult(agent));
            return ExitCodes.Success;
        }

        console.OkLine($"Registered '{agent.Name.EscapeMarkup()}'.");

        return ExitCodes.Success;
    }
}
