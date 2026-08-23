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
        var notifier = services.GetRequiredService<INotifier>();
        var fileSystem = services.GetRequiredService<IFileSystem>();
        var environmentVariableProvider = services.GetRequiredService<IEnvironmentVariableProvider>();
        var resultHolder = services.GetRequiredService<IResultHolder>();

        var messageId = parseResult.GetRequiredValue(Opt<MailMessageIdArgument>.Instance);
        var noPing = parseResult.GetValue(Opt<MailNoPingOption>.Instance);
        var actor = MailActor.Resolve(
            parseResult.GetValue(Opt<MailActorOption>.Instance), environmentVariableProvider);

        var body = await MailBody.ResolveAsync(parseResult, fileSystem, cancellationToken);

        var message = await store.ReplyMessageAsync(messageId, actor, body, cancellationToken);

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
            resultHolder.SetResult(new ObjectResult(MailMessageResult.Create(message)));
            return ExitCodes.Success;
        }

        console.OkLine(
            $"Sent '{message.Id.EscapeMarkup()}' to "
            + $"{string.Join(", ", message.Recipients.Select(recipient => recipient.Name)).EscapeMarkup()}.");

        return ExitCodes.Success;
    }
}
