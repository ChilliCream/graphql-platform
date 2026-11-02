using ChilliCream.Nitro.CommandLine.Commands.Agent.Mail.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Mail;

internal sealed class ArchiveMailCommand : Command
{
    public ArchiveMailCommand() : base("archive")
    {
        Description = "Archive one or more messages for the acting agent.";

        Options.Add(Opt<MailMessagesOption>.Instance);
        Options.Add(Opt<MailActorOption>.Instance);
        Options.Add(Opt<OptionalOutputFormatOption>.Instance);

        this.AddExamples(
            "agent mail archive --message \"m-abc123\" --actor \"maya\"",
            "agent mail archive --message \"m-abc123\" --message \"m-def456\" --actor \"maya\"");

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static async Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken cancellationToken)
    {
        var console = services.GetRequiredService<INitroConsole>();
        var store = services.GetRequiredService<IMailStore>();
        var actorResolver = services.GetRequiredService<IActingActorResolver>();
        var resultHolder = services.GetRequiredService<IResultHolder>();

        var ids = parseResult.GetRequiredValue(Opt<MailMessagesOption>.Instance)
            .Distinct()
            .ToArray();
        var actor = await MailActor.ResolveAsync(
            parseResult.GetValue(Opt<MailActorOption>.Instance), actorResolver, cancellationToken);

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
