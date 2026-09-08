using ChilliCream.Nitro.CommandLine.Commands.Agent.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Tasks;
using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Commands.Agent;

internal sealed class TakeoverAgentCommand : Command
{
    public TakeoverAgentCommand() : base("takeover")
    {
        Description = "Take over another actor's mail and tasks.";

        Subcommands.Add(new TakeoverHistoryAgentCommand());

        Options.Add(Opt<TakeoverFromActorOption>.Instance);
        Options.Add(Opt<TakeoverActorOption>.Instance);
        Options.Add(Opt<ForceActorTakeoverOption>.Instance);
        Options.Add(Opt<TakeoverReasonOption>.Instance);
        Options.Add(Opt<OptionalOutputFormatOption>.Instance);

        this.AddExamples(
            "agent takeover --from \"maya\" --actor \"nora\"",
            "agent takeover --from \"maya\" --actor \"nora\" --force --reason \"session ended\"");

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static async Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken cancellationToken)
    {
        var console = services.GetRequiredService<INitroConsole>();
        var resultHolder = services.GetRequiredService<IResultHolder>();
        var agents = services.GetRequiredService<IAgentRegistry>();
        var sessions = services.GetRequiredService<IAgentSessionRegistry>();
        var mail = services.GetRequiredService<IMailStore>();
        var tasks = services.GetRequiredService<ITaskStore>();
        var ledger = services.GetRequiredService<ITakeoverLedger>();

        var from = MailAgentName.Normalize(
            parseResult.GetValue(Opt<TakeoverFromActorOption>.Instance) ?? string.Empty);
        var to = MailAgentName.Normalize(
            parseResult.GetValue(Opt<TakeoverActorOption>.Instance) ?? string.Empty);
        var force = parseResult.GetValue(Opt<ForceActorTakeoverOption>.Instance);
        var reason = parseResult.GetValue(Opt<TakeoverReasonOption>.Instance);

        var source = await agents.GetAsync(from, cancellationToken)
            ?? throw UnknownActor(from);
        var target = await agents.GetAsync(to, cancellationToken)
            ?? throw UnknownActor(to);

        if (from == to)
        {
            throw new ExitException("The source and target actors must be different.");
        }

        if (!force
            && (await sessions.FindLiveClaimedByAgentNameAsync(from, cancellationToken)).Count > 0)
        {
            throw new ExitException(
                $"Actor '{from}' still has a live session; pass --force to take over anyway.");
        }

        var role = target.Role;
        if (role.Length == 0 && source.Role.Length > 0)
        {
            target = await agents.RegisterAsync(to, source.Role, target.Client, cancellationToken);
            role = target.Role;
        }

        var mailTransfer = await mail.TransferParticipationAsync(from, to, cancellationToken);
        var taskIds = await tasks.ReassignAsync(
            from,
            to,
            to,
            $"Taken over from '{from}' by '{to}'.",
            cancellationToken);
        var items = CreateItems(mailTransfer, taskIds);
        var takeover = await ledger.RecordAsync(
            new TakeoverRecordCreation
            {
                FromActor = from,
                ToActor = to,
                Actor = to,
                Forced = force,
                Role = role.Length > 0 ? role : null,
                Reason = reason
            },
            items,
            cancellationToken);

        if (!console.IsHumanReadable)
        {
            resultHolder.SetResult(new ObjectResult(
                new AgentTakeoverResult(
                    takeover.Id,
                    from,
                    to,
                    role,
                    mailTransfer.RecipientsMoved,
                    mailTransfer.SendersMoved,
                    taskIds)));

            return ExitCodes.Success;
        }

        var taskSummary = taskIds.Count == 0
            ? "no tasks"
            : $"{taskIds.Count} tasks ({string.Join(", ", taskIds.Select(id => id.EscapeMarkup()))})";
        console.OkLine(
            $"'{to.EscapeMarkup()}' took over from '{from.EscapeMarkup()}': "
            + $"role '{role.EscapeMarkup()}', "
            + $"{mailTransfer.RecipientsMoved + mailTransfer.SendersMoved} messages, {taskSummary}.");

        return ExitCodes.Success;
    }

    private static ExitException UnknownActor(string actor)
        => new($"Unknown actor '{actor}'. Run `nitro agent list` to see the actors this workspace knows.");

    private static IReadOnlyList<TakeoverItem> CreateItems(
        MailTransferResult mailTransfer,
        IReadOnlyList<string> taskIds)
    {
        var items = new List<TakeoverItem>(
            mailTransfer.SenderMessageIds.Count
            + mailTransfer.RecipientMessageIds.Count
            + taskIds.Count);
        items.AddRange(mailTransfer.SenderMessageIds.Select(
            id => new TakeoverItem { Kind = TakeoverItemKinds.MessageSender, ItemId = id }));
        items.AddRange(mailTransfer.RecipientMessageIds.Select(
            id => new TakeoverItem { Kind = TakeoverItemKinds.MessageRecipient, ItemId = id }));
        items.AddRange(taskIds.Select(
            id => new TakeoverItem { Kind = TakeoverItemKinds.Task, ItemId = id }));

        return items;
    }

    public sealed record AgentTakeoverResult(
        string Id,
        string From,
        string To,
        string Role,
        int RecipientsMoved,
        int SendersMoved,
        IReadOnlyList<string> Tasks);
}
