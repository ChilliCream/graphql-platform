using ChilliCream.Nitro.CommandLine.Commands.Mail.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Tasks;
using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Commands.Mail;

internal sealed class ReadMailCommand : Command
{
    public ReadMailCommand() : base("read")
    {
        Description = "Print a message and mark it read.";

        Arguments.Add(Opt<MailMessageIdArgument>.Instance);

        Options.Add(Opt<MailThreadOption>.Instance);
        Options.Add(Opt<MailActorOption>.Instance);
        Options.Add(Opt<OptionalOutputFormatOption>.Instance);

        this.AddExamples(
            "agent mail read \"m-abc123\"",
            "agent mail read \"m-abc123\" --thread");

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static async Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken cancellationToken)
    {
        var console = services.GetRequiredService<INitroConsole>();
        var store = services.GetRequiredService<IMailStore>();
        var registry = services.GetRequiredService<IAgentRegistry>();
        var environmentVariableProvider = services.GetRequiredService<IEnvironmentVariableProvider>();
        var resultHolder = services.GetRequiredService<IResultHolder>();

        var messageId = parseResult.GetRequiredValue(Opt<MailMessageIdArgument>.Instance);
        var thread = parseResult.GetValue(Opt<MailThreadOption>.Instance);
        var actor = MailActor.Resolve(
            parseResult.GetValue(Opt<MailActorOption>.Instance), environmentVariableProvider);

        var message = await store.GetRequiredMessageAsync(messageId, cancellationToken);

        RequireParticipant(message, messageId, actor);

        var messages = thread
            ? await MarkThreadReadAsync(store, message.ThreadId, actor, cancellationToken)
            : [await MarkMessageReadAsync(store, message, actor, cancellationToken)];

        if (!console.IsHumanReadable)
        {
            var results = messages.Select(m => MailMessageDetailResult.Create(m, actor)).ToArray();

            resultHolder.SetResult(
                thread
                    ? new ListResult<MailMessageDetailResult>(results)
                    : new ObjectResult(results[0]));

            return ExitCodes.Success;
        }

        for (var i = 0; i < messages.Count; i++)
        {
            if (i > 0)
            {
                console.WriteLine();
                console.WriteLine("---");
                console.WriteLine();
            }

            var sender = await registry.GetAsync(messages[i].Sender, cancellationToken);
            WriteMessage(console, messages[i], sender?.Role ?? "");
        }

        return ExitCodes.Success;
    }

    /// <summary>
    /// Throws <see cref="ExitException"/> when the actor is neither the
    /// message's sender nor one of its recipients.
    /// </summary>
    private static void RequireParticipant(MailMessage message, string messageId, string actor)
    {
        if (message.Sender == actor || message.Recipients.Any(r => r.Name == actor))
        {
            return;
        }

        throw new ExitException(
            $"'{actor}' is not the sender or a recipient of '{messageId}' and cannot read it.");
    }

    private static async Task<MailMessage> MarkMessageReadAsync(
        IMailStore store,
        MailMessage message,
        string actor,
        CancellationToken cancellationToken)
    {
        if (!message.Recipients.Any(r => r.Name == actor))
        {
            return message;
        }

        await store.MarkReadAsync([message.Id], actor, cancellationToken);

        return await store.GetRequiredMessageAsync(message.Id, cancellationToken);
    }

    private static async Task<IReadOnlyList<MailMessage>> MarkThreadReadAsync(
        IMailStore store,
        string threadId,
        string actor,
        CancellationToken cancellationToken)
    {
        var messages = await store.GetThreadMessagesAsync(threadId, cancellationToken);

        var markable = messages
            .Where(m => m.Recipients.Any(r => r.Name == actor))
            .Select(m => m.Id)
            .ToArray();

        if (markable.Length == 0)
        {
            return messages;
        }

        await store.MarkReadAsync(markable, actor, cancellationToken);

        return await store.GetThreadMessagesAsync(threadId, cancellationToken);
    }

    private static void WriteMessage(INitroConsole console, MailMessage message, string senderRole)
    {
        var to = message.Recipients
            .Where(r => r.Kind == MailRecipientKinds.To)
            .OrderBy(r => r.Ordinal)
            .Select(r => r.Name)
            .ToArray();

        var cc = message.Recipients
            .Where(r => r.Kind == MailRecipientKinds.Cc)
            .OrderBy(r => r.Ordinal)
            .Select(r => r.Name)
            .ToArray();

        console.WriteLine(
            senderRole.Length > 0
                ? $"From: {message.Sender} ({senderRole})"
                : $"From: {message.Sender}");
        console.WriteLine($"To: {string.Join(", ", to)}");

        if (cc.Length > 0)
        {
            console.WriteLine($"Cc: {string.Join(", ", cc)}");
        }

        console.WriteLine($"Date: {TaskDates.Format(message.CreatedAt)}");
        console.WriteLine($"Subject: {message.Subject}");
        console.WriteLine($"Thread: {message.ThreadId}");
        console.WriteLine();
        console.WriteLine(message.Body);
    }
}
