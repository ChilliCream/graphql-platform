using ChilliCream.Nitro.CommandLine.Commands.Agent.Mail.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Mail;

internal sealed class ThreadsMailCommand : Command
{
    public ThreadsMailCommand() : base("threads")
    {
        Description = "List threads the acting agent participates in, last activity first. "
            + "Threads with archived messages are included.";

        Options.Add(Opt<MailLimitOption>.Instance);
        Options.Add(Opt<MailActorOption>.Instance);
        Options.Add(Opt<OptionalOutputFormatOption>.Instance);

        this.AddExamples(
            "agent mail threads",
            "agent mail threads --limit 10");

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
        var limit = parseResult.GetValue(Opt<MailLimitOption>.Instance);

        var threads = await store.QueryThreadsAsync(actor, cancellationToken);

        if (limit is not null)
        {
            threads = threads.Take(limit.Value).ToArray();
        }

        var rows = new List<(MailThreadSummary Thread, IReadOnlyList<string> Participants)>(threads.Count);

        foreach (var thread in threads)
        {
            var participants = await ResolveParticipantsAsync(store, thread.ThreadId, cancellationToken);
            rows.Add((thread, participants));
        }

        if (!console.IsHumanReadable)
        {
            resultHolder.SetResult(
                new ListResult<MailThreadRowResult>(
                    rows.Select(r => MailThreadRowResult.Create(r.Thread, r.Participants)).ToArray()));

            return ExitCodes.Success;
        }

        if (rows.Count == 0)
        {
            console.WriteLine("No threads.");
            return ExitCodes.Success;
        }

        var now = timeProvider.GetUtcNow();

        foreach (var (thread, participants) in rows)
        {
            console.WriteLine(FormatRow(thread, participants, now));
        }

        console.WriteLine();
        console.WriteLine($"{rows.Count} thread(s)");

        return ExitCodes.Success;
    }

    private static string FormatRow(
        MailThreadSummary thread, IReadOnlyList<string> participants, DateTimeOffset now)
        => new MailThreadRow
        {
            ThreadId = thread.ThreadId,
            Subject = thread.Subject,
            Participants = participants,
            MessageCount = thread.MessageCount,
            UnreadCount = thread.UnreadCount ?? 0,
            LastActivityAt = thread.LastMessageAt,
            Now = now
        }.Format();

    /// <summary>
    /// Collects the distinct sender and recipient names across every message
    /// in the thread, sorted ordinally.
    /// </summary>
    private static async Task<IReadOnlyList<string>> ResolveParticipantsAsync(
        IMailStore store,
        string threadId,
        CancellationToken cancellationToken)
    {
        var messages = await store.GetThreadMessagesAsync(threadId, cancellationToken);

        var participants = new SortedSet<string>(StringComparer.Ordinal);

        foreach (var message in messages)
        {
            participants.Add(message.Sender);

            foreach (var recipient in message.Recipients)
            {
                participants.Add(recipient.Name);
            }
        }

        return participants.ToArray();
    }
}
