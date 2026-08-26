using ChilliCream.Nitro.CommandLine.Commands.Mail.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Commands.Mail;

internal sealed class InboxMailCommand : Command
{
    public InboxMailCommand() : base("inbox")
    {
        Description = "List messages addressed to the acting agent, newest first.";

        Options.Add(Opt<MailUnreadOption>.Instance);
        Options.Add(Opt<MailFromOption>.Instance);
        Options.Add(Opt<MailSinceOption>.Instance);
        Options.Add(Opt<MailAllOption>.Instance);
        Options.Add(Opt<MailLimitOption>.Instance);
        Options.Add(Opt<MailActorOption>.Instance);
        Options.Add(Opt<OptionalOutputFormatOption>.Instance);

        this.AddExamples(
            "agent mail inbox",
            "agent mail inbox --unread",
            "agent mail inbox --from \"agent-a\" --since \"2026-01-01T00:00:00Z\"");

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

        var actor = await MailActor.ResolveAsync(
            parseResult.GetValue(Opt<MailActorOption>.Instance), actorResolver, cancellationToken);

        var filter = new MailInboxFilter
        {
            Actor = actor,
            UnreadOnly = parseResult.GetValue(Opt<MailUnreadOption>.Instance),
            From = parseResult.GetValue(Opt<MailFromOption>.Instance),
            Since = parseResult.GetValue(Opt<MailSinceOption>.Instance),
            IncludeArchived = parseResult.GetValue(Opt<MailAllOption>.Instance),
            Limit = parseResult.GetValue(Opt<MailLimitOption>.Instance)
        };

        var messages = await store.QueryInboxAsync(filter, cancellationToken);

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
