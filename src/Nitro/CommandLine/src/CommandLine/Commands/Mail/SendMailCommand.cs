using ChilliCream.Nitro.CommandLine.Commands.Mail.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Notify;

namespace ChilliCream.Nitro.CommandLine.Commands.Mail;

internal sealed class SendMailCommand : Command
{
    public SendMailCommand() : base("send")
    {
        Description = "Send a message to one or more agents.";

        Arguments.Add(Opt<MailRecipientsArgument>.Instance);

        Options.Add(Opt<MailSubjectOption>.Instance);
        Options.Add(Opt<MailBodyOption>.Instance);
        Options.Add(Opt<MailBodyFileOption>.Instance);
        Options.Add(Opt<MailCcOption>.Instance);
        Options.Add(Opt<MailActorOption>.Instance);
        Options.Add(Opt<MailNoPingOption>.Instance);
        Options.Add(Opt<OptionalOutputFormatOption>.Instance);

        MailBody.AddValidator(this);

        this.AddExamples(
            "agent mail send \"agent-a\" --subject \"Status\" --body \"All good.\"",
            "agent mail send \"agent-a\" \"agent-b\" --cc \"agent-c\" --subject \"Status\" --body-file notes.txt");

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static async Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken cancellationToken)
    {
        var console = services.GetRequiredService<INitroConsole>();
        var store = services.GetRequiredService<IMailStore>();
        var notifier = services.GetRequiredService<INotifier>();
        var fileSystem = services.GetRequiredService<IFileSystem>();
        var environmentVariableProvider = services.GetRequiredService<IEnvironmentVariableProvider>();
        var resultHolder = services.GetRequiredService<IResultHolder>();

        var to = parseResult.GetRequiredValue(Opt<MailRecipientsArgument>.Instance);
        var cc = parseResult.GetValue(Opt<MailCcOption>.Instance) ?? [];
        var subject = parseResult.GetRequiredValue(Opt<MailSubjectOption>.Instance);
        var noPing = parseResult.GetValue(Opt<MailNoPingOption>.Instance);
        var actor = MailActor.Resolve(
            parseResult.GetValue(Opt<MailActorOption>.Instance), environmentVariableProvider);

        var body = await MailBody.ResolveAsync(parseResult, fileSystem, cancellationToken);

        var message = await store.SendMessageAsync(
            new MailMessageCreation
            {
                Sender = actor,
                Subject = subject,
                Body = body,
                To = to,
                Cc = cc
            },
            cancellationToken);

        // Strictly post-commit: the message is already durably written above,
        // and nothing from here on may alter this command's own exit code or
        // output (the notifier contract - Notifier.NotifyAsync never throws
        // on its own, but this call is wrapped anyway as defense in depth).
        if (!noPing)
        {
            try
            {
                await notifier.NotifyAsync(
                    message.Recipients.Select(recipient => recipient.Name).ToArray(), cancellationToken);
            }
            catch
            {
                // A failed ping is a non-event.
            }
        }

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
