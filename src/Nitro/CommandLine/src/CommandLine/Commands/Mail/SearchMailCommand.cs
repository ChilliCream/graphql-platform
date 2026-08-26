using ChilliCream.Nitro.CommandLine.Commands.Mail.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Commands.Mail;

internal sealed class SearchMailCommand : Command
{
    public SearchMailCommand() : base("search")
    {
        Description = "Search the acting agent's sent and received messages by subject, body, "
            + "and sender, case-insensitively. Archived messages are included.";

        Arguments.Add(Opt<MailSearchTextArgument>.Instance);

        Options.Add(Opt<MailLimitOption>.Instance);
        Options.Add(Opt<MailActorOption>.Instance);
        Options.Add(Opt<OptionalOutputFormatOption>.Instance);

        this.AddExamples(
            "agent mail search \"deploy\"",
            "agent mail search \"deploy\" --limit 10");

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static async Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken cancellationToken)
    {
        var console = services.GetRequiredService<INitroConsole>();
        var store = services.GetRequiredService<IMailStore>();
        var timeProvider = services.GetRequiredService<TimeProvider>();
        var actorResolver = services.GetRequiredService<IActingActorResolver>();
        var resultHolder = services.GetRequiredService<IResultHolder>();

        var text = parseResult.GetRequiredValue(Opt<MailSearchTextArgument>.Instance);
        var actor = await MailActor.ResolveAsync(
            parseResult.GetValue(Opt<MailActorOption>.Instance), actorResolver, cancellationToken);
        var limit = parseResult.GetValue(Opt<MailLimitOption>.Instance);

        var messages = await store.SearchAsync(actor, text, cancellationToken);

        if (limit is not null)
        {
            messages = messages.Take(limit.Value).ToArray();
        }

        if (!console.IsHumanReadable)
        {
            resultHolder.SetResult(
                new ListResult<MailInboxRowResult>(
                    messages.Select(m => MailInboxRowResult.Create(m, actor)).ToArray()));

            return ExitCodes.Success;
        }

        if (messages.Count == 0)
        {
            console.WriteLine("No messages.");
            return ExitCodes.Success;
        }

        var now = timeProvider.GetUtcNow();

        foreach (var message in messages)
        {
            console.WriteLine(FormatRow(message, actor, now));
        }

        console.WriteLine();
        console.WriteLine($"{messages.Count} message(s)");

        return ExitCodes.Success;
    }

    private static string FormatRow(MailMessage message, string actor, DateTimeOffset now)
    {
        var recipient = message.Recipients.FirstOrDefault(r => r.Name == actor);

        return new MailInboxRow
        {
            Id = message.Id,
            Unread = recipient?.ReadAt is null,
            From = message.Sender,
            Subject = message.Subject,
            CreatedAt = message.CreatedAt,
            Now = now
        }.Format();
    }
}
