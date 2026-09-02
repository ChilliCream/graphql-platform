using ChilliCream.Nitro.CommandLine.Commands.Agent.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Tasks;
using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Commands.Agent;

internal sealed class TakeoverHistoryAgentCommand : Command
{
    public TakeoverHistoryAgentCommand() : base("history")
    {
        Description = "List actor takeover history, newest first.";

        Options.Add(Opt<TakeoverHistoryActorOption>.Instance);
        Options.Add(Opt<TakeoverHistoryLimitOption>.Instance);
        Options.Add(Opt<OptionalOutputFormatOption>.Instance);

        this.AddExamples(
            "agent takeover history",
            "agent takeover history --actor \"nora\" --limit 10");

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static async Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken cancellationToken)
    {
        var console = services.GetRequiredService<INitroConsole>();
        var resultHolder = services.GetRequiredService<IResultHolder>();
        var ledger = services.GetRequiredService<ITakeoverLedger>();

        var actorValue = parseResult.GetValue(Opt<TakeoverHistoryActorOption>.Instance);
        var actor = actorValue is null ? null : MailAgentName.Normalize(actorValue);
        var limit = parseResult.GetValue(Opt<TakeoverHistoryLimitOption>.Instance);
        var records = await ledger.QueryAsync(
            new TakeoverFilter { Actor = actor, Limit = limit },
            cancellationToken);
        var results = records.Select(ToResult).ToArray();

        if (!console.IsHumanReadable)
        {
            resultHolder.SetResult(new ListResult<AgentTakeoverHistoryResult>(results));
            return ExitCodes.Success;
        }

        foreach (var result in results)
        {
            console.WriteLine(
                $"{result.Id.EscapeMarkup()}  {TaskDates.Format(result.CreatedAt)}  "
                + $"{result.From.EscapeMarkup()} -> {result.To.EscapeMarkup()}  "
                + $"by {result.Actor.EscapeMarkup()}  "
                + $"{result.MessageSenders + result.MessageRecipients} messages, "
                + $"{result.Tasks.Count} tasks");
        }

        return ExitCodes.Success;
    }

    private static AgentTakeoverHistoryResult ToResult(TakeoverRecord record)
        => new(
            record.Id,
            record.FromActor,
            record.ToActor,
            record.Actor,
            record.CreatedAt,
            record.Forced,
            record.Role,
            record.Reason,
            GetItemCount(record, TakeoverItemKinds.MessageSender),
            GetItemCount(record, TakeoverItemKinds.MessageRecipient),
            GetItemIds(record, TakeoverItemKinds.Task));

    private static int GetItemCount(TakeoverRecord record, string kind)
        => record.Items.Count(item => item.Kind == kind);

    private static IReadOnlyList<string> GetItemIds(TakeoverRecord record, string kind)
        => record.Items
            .Where(item => item.Kind == kind)
            .Select(item => item.ItemId)
            .ToArray();

    public sealed record AgentTakeoverHistoryResult(
        string Id,
        string From,
        string To,
        string Actor,
        DateTimeOffset CreatedAt,
        bool Forced,
        string? Role,
        string? Reason,
        int MessageSenders,
        int MessageRecipients,
        IReadOnlyList<string> Tasks);
}
