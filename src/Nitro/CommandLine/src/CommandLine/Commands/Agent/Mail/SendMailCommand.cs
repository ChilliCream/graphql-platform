using ChilliCream.Nitro.CommandLine.Commands.Agent.Mail.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Notify;
using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Mail;

internal sealed class SendMailCommand : Command
{
    public SendMailCommand() : base("send")
    {
        Description = "Send a message to one or more agents.";

        Options.Add(Opt<MailToOption>.Instance);
        Options.Add(Opt<MailBodyOption>.Instance);
        Options.Add(Opt<MailSubjectOption>.Instance);
        Options.Add(Opt<MailBodyFileOption>.Instance);
        Options.Add(Opt<MailCcOption>.Instance);
        Options.Add(Opt<MailActorOption>.Instance);
        Options.Add(Opt<OptionalOutputFormatOption>.Instance);

        MailBody.AddValidator(this);

        this.AddExamples(
            "agent mail send --to \"agent-a\" --subject \"Status\" --body \"All good.\" --actor \"maya\"",
            "agent mail send --body-file notes.txt --to \"agent-a\" --to \"agent-b\" --cc \"agent-c\" "
            + "--subject \"Status\" --actor \"maya\"");

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

        var to = parseResult.GetRequiredValue(Opt<MailToOption>.Instance);
        var cc = parseResult.GetValue(Opt<MailCcOption>.Instance) ?? [];
        var subject = parseResult.GetRequiredValue(Opt<MailSubjectOption>.Instance);
        var actor = await MailActor.ResolveAsync(
            parseResult.GetValue(Opt<MailActorOption>.Instance), actorResolver, cancellationToken);

        var body = await MailBody.ResolveAsync(parseResult, fileSystem, cancellationToken);

        var message = await store.SendMessageAsync(
            new MailMessageCreation
            {
                Sender = actor,
                Subject = subject,
                Body = body,
                To = to,
                Cc = cc,
                WakePolicy = MailWakePolicy.Enqueue
            },
            cancellationToken);

        // Strictly post-commit: the message is durably written above. The
        // nudge only wakes recipients that have a live session; everyone
        // else sees it when they pull, so it can never fail this command.
        await nudge.NudgeAsync(
            [.. message.Recipients.Select(recipient => recipient.Name)], cancellationToken);

        if (!console.IsHumanReadable)
        {
            resultHolder.SetResult(new ObjectResult(MailSendResult.Create(message)));

            return ExitCodes.Success;
        }

        console.OkLine(
            $"Sent '{message.Id.EscapeMarkup()}' to "
            + $"{string.Join(", ", message.Recipients.Select(recipient => recipient.Name)).EscapeMarkup()}.");

        foreach (var name in message.Unregistered)
        {
            console.WriteLine($"note: '{name}' has never registered.");
        }

        return ExitCodes.Success;
    }
}
