using ChilliCream.Nitro.CommandLine.Commands.Mail.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Notify;

namespace ChilliCream.Nitro.CommandLine.Commands.Mail;

internal sealed class ReplyMailCommand : Command
{
    public ReplyMailCommand() : base("reply")
    {
        Description = "Reply to a message.";

        Arguments.Add(Opt<MailMessageIdArgument>.Instance);

        Options.Add(Opt<MailBodyOption>.Instance);
        Options.Add(Opt<MailBodyFileOption>.Instance);
        Options.Add(Opt<MailActorOption>.Instance);
        Options.Add(Opt<MailNoPingOption>.Instance);
        Options.Add(Opt<OptionalOutputFormatOption>.Instance);

        MailBody.AddValidator(this);

        this.AddExamples(
            "agent mail reply \"m-abc123\" --body \"On it.\"",
            "agent mail reply \"m-abc123\" --body-file reply.txt");

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static async Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken cancellationToken)
    {
        var console = services.GetRequiredService<INitroConsole>();
        var store = services.GetRequiredService<IMailStore>();
        var dispatcher = services.GetRequiredService<IActorWakeDispatcher>();
        var wakeObserver = services.GetRequiredService<IMailWakeReceiptObserver>();
        var timeProvider = services.GetRequiredService<TimeProvider>();
        var fileSystem = services.GetRequiredService<IFileSystem>();
        var environmentVariableProvider = services.GetRequiredService<IEnvironmentVariableProvider>();
        var resultHolder = services.GetRequiredService<IResultHolder>();

        var messageId = parseResult.GetRequiredValue(Opt<MailMessageIdArgument>.Instance);
        var noPing = parseResult.GetValue(Opt<MailNoPingOption>.Instance);
        var actor = MailActor.Resolve(
            parseResult.GetValue(Opt<MailActorOption>.Instance), environmentVariableProvider);

        var body = await MailBody.ResolveAsync(parseResult, fileSystem, cancellationToken);

        var message = await store.ReplyMessageAsync(
            messageId, actor, body, noPing ? MailWakePolicy.Skip : MailWakePolicy.Enqueue, cancellationToken);

        // Strictly post-commit: the message is already durably written above,
        // and nothing from here on can make it not exist. It can still make
        // this command's own exit code and output report the wake truthfully.
        var notification = await MailWakeDispatch.RunAsync(
            message, noPing, dispatcher, wakeObserver, timeProvider, cancellationToken);
        var delivered = WakeReceiptAggregator.IsZero(notification.Status);

        var result = MailMessageResult.Create(message, notification);

        if (!console.IsHumanReadable)
        {
            resultHolder.SetResult(new ObjectResult(result));
            return delivered ? ExitCodes.Success : ExitCodes.Error;
        }

        if (!delivered)
        {
            MailWakeHumanText.WriteStoredButUnconfirmed(console, message, notification, []);
            return ExitCodes.Error;
        }

        console.OkLine(
            $"Sent '{message.Id.EscapeMarkup()}' to "
            + $"{string.Join(", ", message.Recipients.Select(recipient => recipient.Name)).EscapeMarkup()}.");

        MailWakeHumanText.WriteDelivered(console, notification);

        return ExitCodes.Success;
    }
}
