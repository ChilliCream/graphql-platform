using ChilliCream.Nitro.CommandLine.Commands.Agent.Options;
using ChilliCream.Nitro.CommandLine.Commands.Agent.Mail.Options;
using ChilliCream.Nitro.CommandLine.Helpers;
using ChilliCream.Nitro.CommandLine.Results;
using ChilliCream.Nitro.CommandLine.Services;
using ChilliCream.Nitro.CommandLine.Services.Mail;
using ChilliCream.Nitro.CommandLine.Services.Notify;
using ChilliCream.Nitro.CommandLine.Services.Workspace;

namespace ChilliCream.Nitro.CommandLine.Commands.Agent.Mail;

internal sealed class BroadcastMailCommand : Command
{
    public BroadcastMailCommand() : base("broadcast")
    {
        Description = "Send a message to every registered agent except yourself.";

        Options.Add(Opt<MailSubjectOption>.Instance);
        Options.Add(Opt<MailBodyOption>.Instance);
        Options.Add(Opt<MailBodyFileOption>.Instance);
        Options.Add(Opt<RoleAgentOption>.Instance);
        Options.Add(Opt<MailActorOption>.Instance);
        Options.Add(Opt<OptionalOutputFormatOption>.Instance);

        MailBody.AddValidator(this);

        this.AddExamples(
            "agent mail broadcast --subject \"Heads up\" --body \"Deploying at 5pm.\" --actor \"maya\"",
            "agent mail broadcast --role \"backend\" --subject \"Heads up\" --body \"Deploying at 5pm.\" "
            + "--actor \"maya\"");

        this.SetActionWithExceptionHandling(ExecuteAsync);
    }

    private static async Task<int> ExecuteAsync(
        ICommandServices services,
        ParseResult parseResult,
        CancellationToken cancellationToken)
    {
        var console = services.GetRequiredService<INitroConsole>();
        var store = services.GetRequiredService<IMailStore>();
        var agents = services.GetRequiredService<IAgentStore>();
        var nudge = services.GetRequiredService<IMailNudge>();
        var timeProvider = services.GetRequiredService<TimeProvider>();
        var fileSystem = services.GetRequiredService<IFileSystem>();
        var actorResolver = services.GetRequiredService<IActingActorResolver>();
        var resultHolder = services.GetRequiredService<IResultHolder>();

        var subject = parseResult.GetRequiredValue(Opt<MailSubjectOption>.Instance);
        var role = parseResult.GetValue(Opt<RoleAgentOption>.Instance);
        var actor = await MailActor.ResolveAsync(
            parseResult.GetValue(Opt<MailActorOption>.Instance), actorResolver, cancellationToken);

        var to = role is null
            ? await ResolveEveryRegisteredAgentAsync(agents, actor, cancellationToken)
            : await MailRoleRecipients.ResolveAsync(agents, role, actor, cancellationToken);

        if (to.Count is 0)
        {
            throw new ExitException(
                role is null
                    ? "No other registered agent to broadcast to."
                    : $"No agent with role '{role}' to broadcast to.");
        }

        var body = await MailBody.ResolveAsync(parseResult, fileSystem, cancellationToken);

        var message = await store.SendMessageAsync(
            new MailMessageCreation
            {
                Sender = actor,
                Subject = subject,
                Body = body,
                To = to,
                WakePolicy = MailWakePolicy.Enqueue
            },
            cancellationToken);

        // Nudge recipients after the message and wake intent commit.
        await nudge.NudgeAsync(
            [.. message.Recipients.Select(recipient => recipient.Name)], cancellationToken);

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

    /// <summary>
    /// Returns the normalized names, ordered by name, of every non-deleted
    /// registered agent except <paramref name="excludingActor"/>.
    /// </summary>
    private static async Task<IReadOnlyList<string>> ResolveEveryRegisteredAgentAsync(
        IAgentStore agents, string excludingActor, CancellationToken cancellationToken)
    {
        var rows = await agents.ListAsync(cancellationToken);

        return rows
            .Select(agent => agent.Name)
            .Where(name => name != excludingActor)
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToArray();
    }
}
