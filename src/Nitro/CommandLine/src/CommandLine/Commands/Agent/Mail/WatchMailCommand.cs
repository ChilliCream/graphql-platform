using System.Globalization;
using ChilliCream.Nitro.CommandLine.Commands.Agent.Mail.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Tasks;
using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Mail;

internal sealed class WatchMailCommand : Command
{
    private static readonly TimeSpan s_pollInterval = TimeSpan.FromSeconds(1);

    public WatchMailCommand() : base("watch")
    {
        Description = "Wait for new mail addressed to the acting agent and print it. "
            + "Messages already unread at start do not trigger; see `inbox --unread` for those, "
            + "or use --after / --include-existing to deliver them here instead. "
            + "Never marks anything read.";

        Options.Add(Opt<MailTimeoutOption>.Instance);
        Options.Add(Opt<MailAfterOption>.Instance);
        Options.Add(Opt<MailIncludeExistingOption>.Instance);
        Options.Add(Opt<MailActorOption>.Instance);
        Options.Add(Opt<OptionalOutputFormatOption>.Instance);

        this.AddExamples(
            "agent mail watch",
            "agent mail watch --timeout 30",
            "agent mail watch --after 2026-01-01T00:00:00Z",
            "agent mail watch --include-existing");

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
        var timeoutSeconds = parseResult.GetValue(Opt<MailTimeoutOption>.Instance);
        var afterCursor = parseResult.GetValue(Opt<MailAfterOption>.Instance);
        var includeExisting = parseResult.GetValue(Opt<MailIncludeExistingOption>.Instance);

        if (afterCursor is not null && includeExisting)
        {
            throw new ExitException(
                $"Options '{MailAfterOption.OptionName}' and '--include-existing' cannot be combined.");
        }

        var after = afterCursor is null
            ? default(MailCursor?)
            : await ResolveCursorAsync(store, afterCursor, cancellationToken);

        var baseline = await store.QueryInboxAsync(
            new MailInboxFilter { Actor = actor },
            cancellationToken);

        HashSet<string> baselineIds = includeExisting
            ? []
            : after is { } cursor
                ? baseline.Where(m => IsAtOrBeforeCursor(m, cursor)).Select(m => m.Id).ToHashSet()
                : baseline.Select(m => m.Id).ToHashSet();

        var deadline = timeoutSeconds is { } seconds
            ? timeProvider.GetUtcNow() + TimeSpan.FromSeconds(seconds)
            : (DateTimeOffset?)null;

        while (true)
        {
            var delay = s_pollInterval;

            if (deadline is { } d)
            {
                var remaining = d - timeProvider.GetUtcNow();
                delay = remaining < delay
                    ? (remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero)
                    : delay;
            }

            if (delay > TimeSpan.Zero)
            {
                await Task.Delay(delay, timeProvider, cancellationToken);
            }

            var current = await store.QueryInboxAsync(
                new MailInboxFilter { Actor = actor, UnreadOnly = true },
                cancellationToken);

            var arrived = current
                .Where(m => !baselineIds.Contains(m.Id))
                .OrderBy(m => m.CreatedAt)
                .ThenBy(m => m.Id, StringComparer.Ordinal)
                .ToArray();

            if (arrived.Length > 0)
            {
                if (!console.IsHumanReadable)
                {
                    resultHolder.SetResult(
                        new ListResult<MailMessageDetailResult>(
                            arrived.Select(m => MailMessageDetailResult.Create(m, actor)).ToArray()));

                    return ExitCodes.Success;
                }

                PrintMessages(console, arrived);

                return ExitCodes.Success;
            }

            if (deadline is { } dl && timeProvider.GetUtcNow() >= dl)
            {
                console.Error.WriteErrorLine("Timed out waiting for new mail.");
                return ExitCodes.Error;
            }
        }
    }

    private static void PrintMessages(INitroConsole console, IReadOnlyList<MailMessage> messages)
    {
        for (var i = 0; i < messages.Count; i++)
        {
            if (i > 0)
            {
                console.WriteLine();
                console.WriteLine("---");
                console.WriteLine();
            }

            WriteMessage(console, messages[i]);
        }
    }

    private static void WriteMessage(INitroConsole console, MailMessage message)
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

        console.WriteLine($"From: {message.Sender}");
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

    /// <summary>
    /// A point in the message stream a <c>--after</c> cursor resolved to.
    /// <see cref="Id"/> is null for a timestamp cursor, which has no
    /// tiebreak for messages sharing the exact same instant.
    /// </summary>
    private readonly record struct MailCursor(DateTimeOffset CreatedAt, string? Id);

    private static async Task<MailCursor> ResolveCursorAsync(
        IMailStore store,
        string cursor,
        CancellationToken cancellationToken)
    {
        if (DateTimeOffset.TryParse(
            cursor,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal,
            out var timestamp))
        {
            return new MailCursor(timestamp.ToUniversalTime(), null);
        }

        var message = await store.GetMessageAsync(cursor, cancellationToken);

        if (message is null)
        {
            throw new ExitException(
                $"'{MailAfterOption.OptionName}' cursor '{cursor}' is neither a known message ID "
                + "nor a valid RFC 3339 timestamp.");
        }

        return new MailCursor(message.CreatedAt, message.Id);
    }

    /// <summary>
    /// True when <paramref name="message"/> is at or before the cursor, i.e.
    /// it was already visible as of the cursor and must not be re-delivered.
    /// A message cursor excludes exactly the cursor message itself and
    /// everything earlier; a timestamp cursor excludes everything at or
    /// before that instant, having no message to tiebreak against.
    /// </summary>
    private static bool IsAtOrBeforeCursor(MailMessage message, MailCursor cursor)
    {
        if (message.CreatedAt != cursor.CreatedAt)
        {
            return message.CreatedAt < cursor.CreatedAt;
        }

        return cursor.Id is null || string.CompareOrdinal(message.Id, cursor.Id) <= 0;
    }
}
