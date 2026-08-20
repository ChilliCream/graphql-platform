using ChilliCream.Nitro.CommandLine.Commands.Mail.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Tasks;

namespace ChilliCream.Nitro.CommandLine.Commands.Mail;

internal sealed class WatchMailCommand : Command
{
    private static readonly TimeSpan s_pollInterval = TimeSpan.FromSeconds(1);

    public WatchMailCommand() : base("watch")
    {
        Description = "Wait for new mail addressed to the acting agent and print it. "
            + "Messages already unread at start do not trigger; see `inbox --unread` for those. "
            + "Never marks anything read.";

        Options.Add(Opt<MailTimeoutOption>.Instance);
        Options.Add(Opt<MailActorOption>.Instance);
        Options.Add(Opt<OptionalOutputFormatOption>.Instance);

        this.AddExamples(
            "agent mail watch",
            "agent mail watch --timeout 30");

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
        var environmentVariableProvider = services.GetRequiredService<IEnvironmentVariableProvider>();
        var resultHolder = services.GetRequiredService<IResultHolder>();

        var actor = MailActor.Resolve(
            parseResult.GetValue(Opt<MailActorOption>.Instance), environmentVariableProvider);
        var timeoutSeconds = parseResult.GetValue(Opt<MailTimeoutOption>.Instance);

        var baseline = await store.QueryInboxAsync(
            new MailInboxFilter { Actor = actor },
            cancellationToken);
        var baselineIds = baseline.Select(m => m.Id).ToHashSet();

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
}
