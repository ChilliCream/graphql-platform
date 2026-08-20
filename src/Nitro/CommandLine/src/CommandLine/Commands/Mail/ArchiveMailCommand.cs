using ChilliCream.Nitro.CommandLine.Commands.Mail.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Mail;

namespace ChilliCream.Nitro.CommandLine.Commands.Mail;

internal sealed class ArchiveMailCommand : Command
{
    public ArchiveMailCommand() : base("archive")
    {
        Description = "Archive one or more messages for the acting agent.";

        Arguments.Add(Opt<MailMessageIdsArgument>.Instance);

        Options.Add(Opt<MailActorOption>.Instance);
        Options.Add(Opt<OptionalOutputFormatOption>.Instance);

        this.AddExamples(
            "agent mail archive \"m-abc123\"",
            "agent mail archive \"m-abc123\" \"m-def456\"");

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

        var ids = parseResult.GetRequiredValue(Opt<MailMessageIdsArgument>.Instance)
            .Distinct()
            .ToArray();
        var actor = MailActor.Resolve(
            parseResult.GetValue(Opt<MailActorOption>.Instance), environmentVariableProvider);

        await store.ArchiveAsync(ids, actor, cancellationToken);

        if (!console.IsHumanReadable)
        {
            resultHolder.SetResult(new ObjectResult(new MailIdsResult(ids)));
            return ExitCodes.Success;
        }

        foreach (var id in ids)
        {
            console.OkLine($"Archived '{id.EscapeMarkup()}'.");
        }

        return ExitCodes.Success;
    }
}
