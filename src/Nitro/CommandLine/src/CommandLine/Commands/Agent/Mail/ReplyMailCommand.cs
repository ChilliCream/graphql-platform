using ChilliCream.Nitro.CommandLine.Commands.Agent.Mail.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Notify;
using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Mail;

internal sealed class ReplyMailCommand : Command
{
    public ReplyMailCommand() : base("reply")
    {
        Description = "Reply to a message.";

        Options.Add(Opt<MailMessageOption>.Instance);
        Options.Add(Opt<MailBodyOption>.Instance);
        Options.Add(Opt<MailBodyFileOption>.Instance);
        Options.Add(Opt<MailActorOption>.Instance);
        Options.Add(Opt<OptionalOutputFormatOption>.Instance);

        MailBody.AddValidator(this);

        this.AddExamples(
            "agent mail reply --message \"m-abc123\" --body \"On it.\" --actor \"maya\"",
            "agent mail reply --body-file reply.txt --message \"m-abc123\" --actor \"maya\"");

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static async Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken cancellationToken)
    {
        var console = services.GetRequiredService<INitroConsole>();
        var store = services.GetRequiredService<IMailStore>();
        var nudge = services.GetRequiredService<IMailNudge>();
        var timeProvider = services.GetRequiredService<TimeProvider>();
        var fileSystem = services.GetRequiredService<IFileSystem>();
        var actorResolver = services.GetRequiredService<IActingActorResolver>();
        var resultHolder = services.GetRequiredService<IResultHolder>();

        var messageId = parseResult.GetRequiredValue(Opt<MailMessageOption>.Instance);
        var actor = await MailActor.ResolveAsync(
            parseResult.GetValue(Opt<MailActorOption>.Instance), actorResolver, cancellationToken);

        var body = await MailBody.ResolveAsync(parseResult, fileSystem, cancellationToken);

        var message = await store.ReplyMessageAsync(
            messageId, actor, body, MailWakePolicy.Enqueue, cancellationToken);

        // Strictly post-commit: the message is durably written above. The
        // nudge only wakes recipients that have a live session; everyone
        // else sees it when they pull, so it can never fail this command.
        await nudge.NudgeAsync(
            [.. message.Recipients.Select(recipient => recipient.Name)], cancellationToken);

        if (!console.IsHumanReadable)
        {
            resultHolder.SetResult(new ObjectResult(MailMessageResult.Create(message)));

            return ExitCodes.Success;
        }

        console.OkLine(
            $"Sent '{message.Id.EscapeMarkup()}' to "
            + $"{string.Join(", ", message.Recipients.Select(recipient => recipient.Name)).EscapeMarkup()}.");

        return ExitCodes.Success;
    }
}
